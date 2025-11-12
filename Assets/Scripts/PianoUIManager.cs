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
    [SerializeField] private List<string> correctSequence = new List<string>();
    
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
    
    [Header("Configurações de Gameplay")]
    [SerializeField] private bool allowResetOnError = true;
    [SerializeField] private bool autoCloseOnSuccess = true;
    [SerializeField] private float autoCloseDelay = 2f;
    
    [Header("Recompensas")]
    [SerializeField] private string saraRingItemID = "Sara_Ring"; // ID da aliança de Sara
    [SerializeField] private string diaryPage4ID = "DiaryPage_04"; // ID da página 4 do diário
    
    [Header("Áudio")]
    [SerializeField] private string correctSoundName = "puzzle_success";
    [SerializeField] private string incorrectSoundName = "puzzle_error";
    [SerializeField] private string resetSoundName = "ui_click";
    
    private PianoController currentPiano;
    private List<string> currentSequence = new List<string>();
    private bool isSolved = false;
    private bool isProcessing = false;
    private Coroutine feedbackCoroutine;
    
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
    }
    
    /// <summary>
    /// Fecha o painel do puzzle.
    /// </summary>
    public void ClosePuzzle()
    {
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
        if (isSolved || isProcessing) return;
        
        // Validação: verifica se correctSequence foi configurada
        if (correctSequence == null || correctSequence.Count == 0)
        {
            Debug.LogError("Configure a Correct Sequence no PianoUIManager antes de tocar!");
            return;
        }
        
        // Adiciona a nota à sequência atual
        currentSequence.Add(key.NoteName);
        
        UpdateSequenceDisplay();
        
        // Verifica se a sequência está correta até agora
        if (!IsSequenceCorrectSoFar())
        {
            // Sequência incorreta
            if (allowResetOnError)
            {
                StartCoroutine(HandleIncorrectSequence());
            }
        }
        else if (currentSequence.Count == correctSequence.Count)
        {
            // Sequência completa e correta!
            StartCoroutine(HandleCorrectSequence());
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
            if (currentSequence[i] != correctSequence[i])
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Lida com a sequência incorreta.
    /// </summary>
    private IEnumerator HandleIncorrectSequence()
    {
        isProcessing = true;
        
        // Toca som de erro
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(incorrectSoundName))
        {
            AudioManager.Instance.PlaySFX(incorrectSoundName);
        }
        
        // Mostra feedback
        ShowFeedback("Sequência incorreta! Resetando...", incorrectColor, feedbackDuration);
        
        yield return new WaitForSeconds(1f);
        
        // Reseta a sequência
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
        
        // Toca som de sucesso
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(correctSoundName))
        {
            AudioManager.Instance.PlaySFX(correctSoundName);
        }
        
        // Mostra feedback
        ShowFeedback("Sequência correta! O piano se abre...", correctColor, feedbackDuration);
        
        yield return new WaitForSeconds(feedbackDuration);
        
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
            yield return new WaitForSeconds(autoCloseDelay);
            ClosePuzzle();
        }
        
        isProcessing = false;
    }
    
    /// <summary>
    /// Reseta a sequência atual de notas.
    /// </summary>
    public void ResetSequence()
    {
        currentSequence.Clear();
        UpdateSequenceDisplay();
        
        // Toca som de reset
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(resetSoundName))
        {
            AudioManager.Instance.PlaySFX(resetSoundName);
        }
        
        // Força todas as teclas a soltar (caso necessário)
        PianoKey[] allKeys = FindObjectsByType<PianoKey>(FindObjectsSortMode.None);
        foreach (var key in allKeys)
        {
            key.ForceRelease();
        }
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
