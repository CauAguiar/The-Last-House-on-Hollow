using UnityEngine;

/// <summary>
/// Controlador do piano no mundo do jogo.
/// Gerencia a interação com o piano e revela as recompensas quando o puzzle é resolvido.
/// </summary>
public class PianoController : InteractableBase
{
    [Header("Configuração do Piano")]
    [SerializeField] private string pianoID = "MainHallPiano";
    [SerializeField] private bool isPuzzleSolved = false;
    
    [Header("Recompensas (GameObjects no mundo)")]
    [SerializeField] private GameObject saraRingObject; // Aliança de Sara (GameObject no mundo)
    [SerializeField] private GameObject diaryPage4Object; // Página 4 do Diário (GameObject no mundo)
    [SerializeField] private GameObject pianoLidClosed; // Tampa do piano fechada
    [SerializeField] private GameObject pianoLidOpen; // Tampa do piano aberta
    
    [Header("Áudio")]
    [SerializeField] private string pianoOpenSoundName = "piano_open";
    
    [Header("Animação (Opcional)")]
    [SerializeField] private Animator pianoAnimator;
    [SerializeField] private string openAnimationTrigger = "Open";
    
    protected override void Awake()
    {
        base.Awake();
        
        // Carrega o estado salvo
        LoadState();
        
        // Configura o estado inicial
        UpdateVisuals();
    }
    
    private void Start()
    {
        // Esconde as recompensas inicialmente
        if (saraRingObject != null)
        {
            saraRingObject.SetActive(isPuzzleSolved);
        }
        
        if (diaryPage4Object != null)
        {
            diaryPage4Object.SetActive(isPuzzleSolved);
        }
    }
    
    public override void Interact()
    {
        if (isPuzzleSolved)
        {
            // Se já resolvido, mostra uma mensagem
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.ShowDialogue("O piano já está aberto. As recompensas foram reveladas.");
            }
            return;
        }
        
        // Abre o painel do puzzle do piano
        if (PianoUIManager.Instance != null)
        {
            PianoUIManager.Instance.OpenPianoPuzzle(this);
        }
        else
        {
            Debug.LogError("PianoUIManager não encontrado na cena!");
        }
    }
    
    public override void OnInspect()
    {
        if (isPuzzleSolved)
        {
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.ShowDialogue("Um piano antigo. A tampa está aberta, revelando um compartimento secreto.");
            }
        }
        else
        {
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.ShowDialogue("Um piano antigo no Salão Principal. Parece que esconde algo...");
            }
        }
    }
    
    /// <summary>
    /// Chamado pelo PianoUIManager quando o puzzle é resolvido.
    /// </summary>
    public void OnPuzzleSolved()
    {
        if (isPuzzleSolved) return;
        
        isPuzzleSolved = true;
        
        // Salva o estado
        SaveState();
        
        // Toca animação de abertura
        PlayOpenAnimation();
        
        // Atualiza visuais
        UpdateVisuals();
        
        // Revela as recompensas
        RevealRewards();
        
        // Toca som de abertura
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(pianoOpenSoundName))
        {
            AudioManager.Instance.PlaySFX(pianoOpenSoundName);
        }
        
        Debug.Log("Piano resolvido! Recompensas reveladas.");
    }
    
    /// <summary>
    /// Toca a animação de abertura do piano.
    /// </summary>
    private void PlayOpenAnimation()
    {
        if (pianoAnimator != null && !string.IsNullOrEmpty(openAnimationTrigger))
        {
            pianoAnimator.SetTrigger(openAnimationTrigger);
        }
    }
    
    /// <summary>
    /// Atualiza os visuais do piano (tampa aberta/fechada).
    /// </summary>
    private void UpdateVisuals()
    {
        if (pianoLidClosed != null)
        {
            pianoLidClosed.SetActive(!isPuzzleSolved);
        }
        
        if (pianoLidOpen != null)
        {
            pianoLidOpen.SetActive(isPuzzleSolved);
        }
    }
    
    /// <summary>
    /// Revela as recompensas no mundo.
    /// </summary>
    private void RevealRewards()
    {
        if (saraRingObject != null)
        {
            saraRingObject.SetActive(true);
        }
        
        if (diaryPage4Object != null)
        {
            diaryPage4Object.SetActive(true);
        }
    }
    
    /// <summary>
    /// Salva o estado do piano.
    /// </summary>
    private void SaveState()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.MarkAsCollected(pianoID + "_Solved");
        }
    }
    
    /// <summary>
    /// Carrega o estado do piano.
    /// </summary>
    private void LoadState()
    {
        if (GameStateManager.Instance != null)
        {
            isPuzzleSolved = GameStateManager.Instance.IsCollected(pianoID + "_Solved");
        }
    }
    
    /// <summary>
    /// Reseta o estado do piano (para debug/teste).
    /// </summary>
    [ContextMenu("Reset Piano")]
    public void ResetPiano()
    {
        isPuzzleSolved = false;
        UpdateVisuals();
        
        if (saraRingObject != null)
        {
            saraRingObject.SetActive(false);
        }
        
        if (diaryPage4Object != null)
        {
            diaryPage4Object.SetActive(false);
        }
        
        Debug.Log("Piano resetado!");
    }
}
