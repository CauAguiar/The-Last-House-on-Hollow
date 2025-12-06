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
    // UI fields removed: resetButton, feedbackText, sequenceDisplayText
    
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
    [SerializeField] private string saraRingItemID = "Sara_Ring"; // ID da aliança de Sara (mantido apenas para compatibilidade)
    [Tooltip("ID numérico da página do diário a desbloquear quando o piano for resolvido. Use o pageId definido em JournalData.")]
    [SerializeField] private int diaryPage4Id = 4; // pageId da página 4 do diário
    [Header("Recompensas (InventoryItems)")]
    [Tooltip("Arraste aqui o InventoryItem (ScriptableObject) da aliança de Sara para adicioná-la ao inventário quando o piano for resolvido.")]
    [SerializeField] private InventoryItem saraRingItem = null;
    
    // Áudio: removido, gerenciado por sistema global se necessário

    [Header("Controle Global de Notas")]
    [Tooltip("Se true, aplica a duração global de nota a todas as teclas ao abrir o puzzle.")]
    public bool useGlobalNoteDuration = false;
    [Tooltip("Duração padrão em segundos que cada nota deve durar quando useGlobalNoteDuration == true.")]
    public float globalNoteDuration = 1.11f;
    [Tooltip("Se true, aplica um fade out no final da nota quando a duração global é usada.")]
    public bool enableNoteFade = true;
    [Tooltip("Duração do fade out em segundos (aplicada ao final da nota).")]
    public float noteFadeOutDuration = 0.15f;
    
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
        
        // resetButton removed: no listener to register
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
            // PianoUIManager: 'Correct Sequence' não estava configurada; usando sequência padrão. (log removed)
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
            return;
        }
        
        currentPiano = piano;
        currentSequence.Clear();
        
        if (pianoPanel != null)
        {
            pianoPanel.SetActive(true);
        }

        // Apply global note duration/fade settings to all keys when opening the puzzle
        if (useGlobalNoteDuration)
        {
            PianoKey[] allKeys;
#if UNITY_2023_1_OR_NEWER
            allKeys = UnityEngine.Object.FindObjectsByType<PianoKey>(FindObjectsSortMode.None);
#else
            allKeys = FindObjectsOfType<PianoKey>(true);
#endif
            foreach (var k in allKeys)
            {
                if (k == null) continue;
                k.SetNoteSoundSlice(0f, globalNoteDuration);
            }
        }
        
        // Bloqueia o movimento do jogador
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.LockMovement();
        }
        
        UIInputBlocker.Block("PianoPuzzle");
        GamePauseManager.Pause("PianoPuzzle");
        
        UpdateSequenceDisplay();
        LogPianoKeyOrder();
        RemapKeysIfInverted();
        // Ensure close button is enabled unless we are processing (blocking)
        if (closeButton != null)
            closeButton.interactable = !isProcessing;

        // Accessibility: select close button so keyboard users can press Escape or submit to close
        if (UnityEngine.EventSystems.EventSystem.current != null && closeButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        if (pianoPanel != null && pianoPanel.GetComponent<UIAutoCloseOnCancel>() == null)
        {
            var helper = pianoPanel.AddComponent<UIAutoCloseOnCancel>();
            helper.panel = pianoPanel;
            helper.closeButton = closeButton;
        }
    }

    // Debugging helper: logs order of PianoKey objects sorted by X position (left-to-right)
    private void LogPianoKeyOrder()
    {
        PianoKey[] allKeys;
    #if UNITY_2023_1_OR_NEWER
        allKeys = UnityEngine.Object.FindObjectsByType<PianoKey>(FindObjectsSortMode.None);
    #else
        allKeys = FindObjectsOfType<PianoKey>(true);
    #endif
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
        // Debug output removed: order
    }

    // If the keys are visually in descending pitch order (left-to-right), remap note names
    // so that leftmost is lowest pitch. This fixes inverted layouts created in the editor
    // or by other scripts at runtime.
    private void RemapKeysIfInverted()
    {
        PianoKey[] allKeys;
    #if UNITY_2023_1_OR_NEWER
        allKeys = UnityEngine.Object.FindObjectsByType<PianoKey>(FindObjectsSortMode.None);
    #else
        allKeys = FindObjectsOfType<PianoKey>(true);
    #endif
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
            // Detectei ordem invertida — remap aplicado (log removed)
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

        // Debug: log da tecla pressionada e sequência atual para ajudar a diagnosticar mismatches
        string pressed = key != null ? key.NoteName : "(null)";
        // PianoUIManager: Key pressed (log removed)

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
                // No visual feedback for errors while processing.
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
            // Sequência incorreta: apenas reseta a sequência sem feedback visual.
            ResetSequence();
        }
        else if (currentSequence.Count == correctSequence.Count)
        {
            // Sequência completa e correta! (feedback textual apenas)
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
            // Correto até aqui — feedback textual apenas; não alteramos a cor da tecla
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
        // Apenas espera um pequeno intervalo para manter consistência com processamento
        yield return null;
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
        
        
        // Mostra feedback (sucesso)
        ShowFeedback("Sequência correta! O piano se abre...", Color.white, feedbackDuration);
        
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
        PianoKey[] allKeys;
    #if UNITY_2023_1_OR_NEWER
        allKeys = UnityEngine.Object.FindObjectsByType<PianoKey>(FindObjectsSortMode.None);
    #else
        allKeys = FindObjectsOfType<PianoKey>(true);
    #endif
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
        // sequenceDisplayText removed: no UI sequence display to update
    }
    
    /// <summary>
    /// Mostra mensagem de feedback ao jogador.
    /// </summary>
    private void ShowFeedback(string message, Color color, float duration)
    {
        // feedbackText removed: no onscreen textual feedback in this manager anymore.
        // Kept method as a no-op to avoid changing many call sites.
    }
    
    /// <summary>
    /// Limpa o texto de feedback após um delay.
    /// </summary>
    private IEnumerator ClearFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // no-op: feedbackText removed
    }
    
    /// <summary>
    /// Dá as recompensas ao jogador (Aliança de Sara e Página 4 do Diário).
    /// </summary>
    private void GiveRewards()
    {
        // Preferir adicionar InventoryItems ao inventário (assets do tipo InventoryItem)
        if (InventoryManager.Instance != null)
        {
            if (saraRingItem != null)
            {
                InventoryManager.Instance.AddItem(saraRingItem);
                // Aliança de Sara adicionada ao inventário (log removed)
            }

            // Add diary page by ID via JournalManager
            if (JournalManager.Instance != null && diaryPage4Id > 0)
            {
                JournalManager.Instance.CollectPage(diaryPage4Id);
                // Página do Diário adicionada via JournalManager (log removed)
            }
        }

        // Fallback/compatibilidade: ainda marca IDs no GameState caso o designer prefira usar IDs
        if (GameStateManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(saraRingItemID)) GameStateManager.Instance.MarkAsCollected(saraRingItemID);
            if (diaryPage4Id > 0) GameStateManager.Instance.MarkAsCollected(diaryPage4Id.ToString());
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
