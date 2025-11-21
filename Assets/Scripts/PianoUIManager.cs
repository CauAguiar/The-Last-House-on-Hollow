using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerencia o painel UI do puzzle do piano.
/// Controla a sequência de notas tocadas, valida a solução e gerencia recompensas.
/// </summary>
public class PianoUIManager : MonoBehaviour
{
    public static PianoUIManager Instance;
    
    [Header("Configuração da Solução")]
    [Tooltip("Sequência correta de notas que deve ser tocada (ex: C, D, E, F, G)")]
    [SerializeField] private List<string> correctSequence = new List<string>() { "B", "A#", "G", "A#", "G" };
    [Tooltip("Se true e a lista 'Correct Sequence' estiver vazia no Inspector, será usada uma sequência padrão (B, A#, G, A#, G)")]
    [SerializeField] private bool useDefaultSequenceIfEmpty = true;
    
    [Header("Referências da UI")]
    [SerializeField] private GameObject pianoPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI sequenceDisplayText; // Mostra a sequência atual
    
    [Header("Feedback Visual")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private float feedbackDuration = 2f;
    [Header("Áudio")]
    [SerializeField] private string successSfxName = ""; // SFX tocado ao completar a sequência
    
    [Header("Configurações de Gameplay")]
    [SerializeField] private bool allowResetOnError = true;
    [SerializeField] private bool autoCloseOnSuccess = true;
    [SerializeField] private float autoCloseDelay = 2f;
    [Tooltip("If true, the current sequence will be reset after a period of inactivity. Default false to avoid accidental expiration.")]
    [SerializeField] private bool enableInactivityReset = false;
    [Tooltip("Seconds of inactivity before the sequence is reset when enabled.")]
    [SerializeField] private float inactivityResetSeconds = 30f;
    
    [Header("Recompensas")]
    [SerializeField] private string saraRingItemID = "Sara_Ring"; // ID da aliança de Sara
    [SerializeField] private string diaryPage4ID = "DiaryPage_04"; // ID da página 4 do diário
    
    // Áudio: removido, gerenciado por sistema global se necessário
    
    private PianoController currentPiano;
    private List<string> currentSequence = new List<string>();
    private bool isSolved = false;
    private bool isProcessing = false;
    private Coroutine feedbackCoroutine;
    private Coroutine inactivityCoroutine;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Configura botões
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePuzzle);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetSequence);
        }
    }
    
    private void Start()
    {
        if (pianoPanel != null)
        {
            pianoPanel.SetActive(false);
        }
        // Se a sequência correta não foi configurada via Inspector, opcionalmente usamos a sequência padrão
        if ((correctSequence == null || correctSequence.Count == 0) && useDefaultSequenceIfEmpty)
        {
            correctSequence = new List<string>() { "B", "A#", "G", "A#", "G" };
            Debug.LogWarning("PianoUIManager: 'Correct Sequence' não estava configurada; usando sequência padrão: B, A#, G, A#, G.");
        }

        UpdateSequenceDisplay();
    }
    
    /// <summary>
    /// Abre o painel do puzzle do piano.
    /// </summary>
    public void OpenPianoPuzzle(PianoController piano)
    {
        if (isSolved)
        {
            ShowFeedback("O piano já foi resolvido!", normalColor, 2f);
            return;
        }
        
        currentPiano = piano;
        currentSequence.Clear();
        
        if (pianoPanel != null)
        {
            pianoPanel.SetActive(true);
        }
        
        // Bloqueia o movimento do jogador
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.LockMovement();
        }
        
        UIInputBlocker.Block("PianoPuzzle");
        GamePauseManager.Pause("PianoPuzzle");
        
        UpdateSequenceDisplay();
        ShowFeedback("Toque a sequência correta de notas.", normalColor, 3f);
        // Debug: log key order by position so we can verify mapping
        LogPianoKeyOrder();
        RemapKeysIfInverted();
        // Ensure close button is enabled unless we are processing (blocking)
        if (closeButton != null)
            closeButton.interactable = !isProcessing;
    }

    // Debugging helper: logs order of PianoKey objects sorted by X position (left-to-right)
    private void LogPianoKeyOrder()
    {
        PianoKey[] allKeys = FindObjectsByType<PianoKey>(FindObjectsSortMode.None);
        System.Array.Sort(allKeys, (a, b) =>
        {
            RectTransform ra = a.GetComponent<RectTransform>();
            RectTransform rb = b.GetComponent<RectTransform>();
            if (ra == null || rb == null) return 0;
            return ra.anchoredPosition.x.CompareTo(rb.anchoredPosition.x);
        });

        string order = "Piano key order (left to right): ";
        foreach (var k in allKeys)
        {
            RectTransform r = k.GetComponent<RectTransform>();
            order += $"{k.NoteName}@{r.anchoredPosition.x:F1} (sibling={k.transform.GetSiblingIndex()}), ";
        }
        Debug.Log(order);
    }

    // If the keys are visually in descending pitch order (left-to-right), remap note names
    // so that leftmost is lowest pitch. This fixes inverted layouts created in the editor
    // or by other scripts at runtime.
    private void RemapKeysIfInverted()
    {
        PianoKey[] allKeys = FindObjectsByType<PianoKey>(FindObjectsSortMode.None);
        if (allKeys == null || allKeys.Length == 0) return;

        // Sort left-to-right by anchored X
        System.Array.Sort(allKeys, (a, b) => a.GetComponent<RectTransform>().anchoredPosition.x.CompareTo(
            b.GetComponent<RectTransform>().anchoredPosition.x));

        // Convert NoteNames to numeric pitch values
        List<int> pitches = new List<int>();
        foreach (var k in allKeys)
        {
            int p = NoteNameToPitchValue(k.NoteName);
            pitches.Add(p);
        }

        // Check if the sequence is reversed (first > last)
        if (pitches.Count >= 2 && pitches[0] > pitches[pitches.Count - 1])
        {
            Debug.Log("Detectei ordem invertida — aplicando remap de notas para left-to-right asc.");
            List<int> sortedPitches = new List<int>(pitches);
            sortedPitches.Sort();

            // Remap each key to the corresponding ascending pitch
            for (int i = 0; i < allKeys.Length; i++)
            {
                string newName = PitchValueToNoteName(sortedPitches[i]);
                allKeys[i].SetNoteName(newName);
                allKeys[i].gameObject.name = "Key_" + newName; // keep GameObject name consistent
            }
        }
    }

    private static readonly string[] NOTE_NAMES = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    // Converts "C#4" -> integer pitch (octave*12 + noteIndex)
    private int NoteNameToPitchValue(string note)
    {
        if (string.IsNullOrEmpty(note)) return 0;
        // extract numeric suffix
        int idx = note.Length - 1;
        while (idx >= 0 && char.IsDigit(note[idx])) idx--;
        string baseNote = note.Substring(0, idx + 1);
        int octave = 0;
        if (idx + 1 < note.Length)
            int.TryParse(note.Substring(idx + 1), out octave);
        int noteIndex = System.Array.IndexOf(NOTE_NAMES, baseNote);
        if (noteIndex < 0) noteIndex = 0;
        return octave * 12 + noteIndex;
    }

    private string PitchValueToNoteName(int pitch)
    {
        int noteIndex = Mathf.FloorToInt(pitch % 12);
        int octave = Mathf.FloorToInt(pitch / 12);
        return NOTE_NAMES[noteIndex] + octave.ToString();
    }
    
    /// <summary>
    /// Fecha o painel do puzzle.
    /// </summary>
    /// <summary>
    /// Fecha o painel do puzzle.
    /// This wrapper is callable by UI (no parameters). It will respect the
    /// processing/solved guard and refuse to close when a success flow is running.
    /// </summary>
    public void ClosePuzzle()
    {
        ClosePuzzleInternal(false);
    }

    /// <summary>
    /// Force-close the puzzle UI even if a success processing flow is running.
    /// Used by code paths that must close the UI programmatically (e.g. auto-close).
    /// </summary>
    public void ClosePuzzleForce()
    {
        ClosePuzzleInternal(true);
    }

    private void ClosePuzzleInternal(bool force)
    {
        if (isProcessing && isSolved && !force)
        {
            // While success processing is running, prevent the player from closing the puzzle
            ShowFeedback("Aguarde...", normalColor, 1f);
            return;
        }
        if (pianoPanel != null)
        {
            pianoPanel.SetActive(false);
        }
        
        currentPiano = null;
        
        // Desbloqueia o movimento do jogador
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.UnlockMovement();
        }
        
        UIInputBlocker.Unblock("PianoPuzzle");
        GamePauseManager.Unpause("PianoPuzzle");
        
        // Reseta a sequência ao fechar
        ResetSequence();
    }
    
    /// <summary>
    /// Chamado quando uma tecla do piano é pressionada.
    /// </summary>
    public void OnKeyPressed(PianoKey key)
    {
        if (isSolved) return;

        // If a reset is already in progress, allow color feedback (so player sees red/green)
        // but don't modify the sequence or trigger logic until the reset completes.
        if (isProcessing)
        {
            // If we are processing a CORRECT sequence, fully block keys (no feedback)
            if (isSolved)
                return;

            if (key != null)
            {
                // While processing (a reset is in progress), do not allow green feedback.
                // Always flash incorrect so the player knows the sequence is locked
                float flashDuration = feedbackDuration;
                if (key.Type == PianoKey.KeyType.Black)
                    flashDuration = key.PressedDuration;
                key.FlashColor(incorrectColor, flashDuration);
            }

            return;
        }
        
        // Validação: verifica se correctSequence foi configurada
        if (correctSequence == null || correctSequence.Count == 0)
        {
            // Ainda não configurada: se não usarmos fallback, avisamos o desenvolvedor e não processamos a tecla
            Debug.LogError("Configure a Correct Sequence no PianoUIManager antes de tocar!");
            return;
        }
        
        // Se a tecla já tem cor persistente, removemos e reaplicamos para "pintar de novo"
        if (key != null && key.HasPersistentColor())
        {
            key.ForceRelease();
        }

        // Adiciona a nota à sequência atual
        currentSequence.Add(key.NoteName);
        // On any key press, restart inactivity timer
        if (enableInactivityReset)
        {
            if (inactivityCoroutine != null) StopCoroutine(inactivityCoroutine);
            inactivityCoroutine = StartCoroutine(ResetSequenceAfterInactivity(inactivityResetSeconds));
        }
        
        UpdateSequenceDisplay();
        
        // Verifica se a sequência está correta até agora
        if (!IsSequenceCorrectSoFar())
        {
            // Sequência incorreta
            // Mostra feedback direto na tecla (vermelho)
            if (key != null)
            {
                float flashDuration = feedbackDuration;
                if (key.Type == PianoKey.KeyType.Black)
                    flashDuration = key.PressedDuration;

                // Reset sequence immediately so any subsequent click is treated as a fresh start.
                ResetSequence();

                // Still flash the wrong key for visual feedback (after reset)
                key.FlashColor(incorrectColor, flashDuration);
            }
            // Show UI feedback but do not delay reset (we already cleared the sequence)
            if (allowResetOnError)
            {
                ShowFeedback("Sequência incorreta! Resetando...", incorrectColor, feedbackDuration);
            }
        }
        else if (currentSequence.Count == correctSequence.Count)
        {
            // Sequência completa e correta!
            // Mostra feedback direto na tecla (verde) e mantém a cor até reset
            if (key != null)
            {
                key.SetPersistentColor(correctColor);
            }
            // Toca som de sucesso, se configurado
            if (!string.IsNullOrEmpty(successSfxName) && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(successSfxName);
            }
            // Disable close button during the success feedback
            if (closeButton != null) closeButton.interactable = false;
            StartCoroutine(HandleCorrectSequence());
        }
        else
        {
            // Correto até aqui — pinta a tecla de verde e mantém a cor
            if (key != null)
            {
                key.SetPersistentColor(correctColor);
            }
        }
    }
    
    /// <summary>
    /// Verifica se a sequência atual está correta até o momento.
    /// </summary>
    private bool IsSequenceCorrectSoFar()
    {
        // Validação: verifica se correctSequence não está vazia
        if (correctSequence == null || correctSequence.Count == 0)
        {
            Debug.LogError("Sequência correta não foi configurada no PianoUIManager!");
            return false;
        }
        
        // Validação: não pode ter mais notas que a sequência correta
        if (currentSequence.Count > correctSequence.Count)
        {
            return false;
        }
        
        for (int i = 0; i < currentSequence.Count; i++)
        {
            string currentNote = NormalizeNoteName(currentSequence[i]);
            string correctNote = NormalizeNoteName(correctSequence[i]);

            if (!string.Equals(currentNote, correctNote, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Normaliza o nome da nota removendo o número da oitava (ex: "A#4" -> "A#").
    /// Isso permite que a solução seja verificada apenas pela nota musical, sem considerar a oitava.
    /// </summary>
    private string NormalizeNoteName(string note)
    {
        if (string.IsNullOrEmpty(note)) return note;

        // Remove todos os dígitos no final da string (ex: "G4" -> "G")
        int idx = note.Length - 1;
        while (idx >= 0 && char.IsDigit(note[idx])) idx--;

        return note.Substring(0, idx + 1).Trim();
    }
    
    /// <summary>
    /// Lida com a sequência incorreta.
    /// </summary>
    private IEnumerator HandleIncorrectSequence()
    {
        isProcessing = true;
        
        
        // Mostra feedback
        ShowFeedback("Sequência incorreta! Resetando...", incorrectColor, feedbackDuration);

        // Aguarda o tempo de feedback para que a cor vermelha seja exibida
        // Adiciona um pequeno intervalo extra para garantir que o flash tenha terminado
        yield return new WaitForSecondsRealtime(feedbackDuration + 0.05f);

        // Depois que o feedback foi exibido, reseta a sequência
        ResetSequence();
        
        isProcessing = false;
    }
    
    /// <summary>
    /// Lida com a sequência correta (puzzle resolvido).
    /// </summary>
    private IEnumerator HandleCorrectSequence()
    {
        isProcessing = true;
        isSolved = true;
        
        
        // Mostra feedback
        ShowFeedback("Sequência correta! O piano se abre...", correctColor, feedbackDuration);
        
        yield return new WaitForSecondsRealtime(feedbackDuration);
        
        // Dá as recompensas
        GiveRewards();
        
        // Notifica o piano controller
        if (currentPiano != null)
        {
            currentPiano.OnPuzzleSolved();
        }
        
        // Fecha automaticamente se configurado
        if (autoCloseOnSuccess)
        {
            // Use unscaled time so auto-close works even if the game is paused (timeScale == 0)
            yield return new WaitForSecondsRealtime(autoCloseDelay);
            ClosePuzzleForce();
        }
        // Re-enable close button after processing finished (if puzzle remains open)
        if (closeButton != null)
            closeButton.interactable = true;
        
        isProcessing = false;
    }
    
    /// <summary>
    /// Reseta a sequência atual de notas.
    /// </summary>
    public void ResetSequence()
    {
        currentSequence.Clear();
        UpdateSequenceDisplay();
        
        
        // Força todas as teclas a soltar (caso necessário)
        PianoKey[] allKeys = FindObjectsByType<PianoKey>(FindObjectsSortMode.None);
        foreach (var key in allKeys)
        {
            key.ForceRelease();
        }

        // Make sure the close button is enabled again after reset
        if (closeButton != null)
            closeButton.interactable = true;

        // stop inactivity reset when sequence cleared
        if (inactivityCoroutine != null)
        {
            StopCoroutine(inactivityCoroutine);
            inactivityCoroutine = null;
        }
    }

    private IEnumerator ResetSequenceAfterInactivity(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ResetSequence();
        inactivityCoroutine = null;
    }
    
    /// <summary>
    /// Atualiza o display da sequência atual.
    /// </summary>
    private void UpdateSequenceDisplay()
    {
        if (sequenceDisplayText != null)
        {
            string display = "Sequência: ";
            if (currentSequence.Count == 0)
            {
                display += "-";
            }
            else
            {
                display += string.Join(", ", currentSequence);
            }
            
            display += $" ({currentSequence.Count}/{correctSequence.Count})";
            sequenceDisplayText.text = display;
        }
    }
    
    /// <summary>
    /// Mostra mensagem de feedback ao jogador.
    /// </summary>
    private void ShowFeedback(string message, Color color, float duration)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
            
            // Cancela corrotina anterior se existir
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }
            
            feedbackCoroutine = StartCoroutine(ClearFeedbackAfterDelay(duration));
        }
    }
    
    /// <summary>
    /// Limpa o texto de feedback após um delay.
    /// </summary>
    private IEnumerator ClearFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }
    
    /// <summary>
    /// Dá as recompensas ao jogador (Aliança de Sara e Página 4 do Diário).
    /// </summary>
    private void GiveRewards()
    {
        // Adiciona a Aliança de Sara ao inventário
        if (!string.IsNullOrEmpty(saraRingItemID) && InventoryManager.Instance != null)
        {
            // Você precisará criar um InventoryItem para a aliança
            // Por enquanto, apenas marcamos como coletado
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.MarkAsCollected(saraRingItemID);
            }
            
            Debug.Log("Aliança de Sara adicionada ao inventário!");
        }
        
        // Adiciona a Página 4 do Diário
        if (!string.IsNullOrEmpty(diaryPage4ID))
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.MarkAsCollected(diaryPage4ID);
            }
            
            // Se você tiver um sistema de diário, adicione a página aqui
            // JournalManager.Instance.UnlockPage(diaryPage4ID);
            
            Debug.Log("Página 4 do Diário desbloqueada!");
        }
    }
    
    /// <summary>
    /// Define a sequência correta de notas (útil para configurar dinamicamente).
    /// </summary>
    public void SetCorrectSequence(List<string> sequence)
    {
        correctSequence = new List<string>(sequence);
    }
    
    /// <summary>
    /// Retorna se o puzzle já foi resolvido.
    /// </summary>
    public bool IsSolved()
    {
        return isSolved;
    }
}
