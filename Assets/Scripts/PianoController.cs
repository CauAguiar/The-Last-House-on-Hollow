using UnityEngine;
using UnityEngine.UI;

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
    [Header("Lid Visual (Sprite mode)")]
    [Tooltip("Se true, usa sprites para substituir o visual do tampo em vez de ativar/desativar os objetos de lid."
        + " Você pode arrastar um SpriteRenderer do GameObject do tampo do piano ou uma Image UI.)")]
    [SerializeField] private bool useSpriteForLid = false;
    [Tooltip("Se você usa um SpriteRenderer para o tampo do piano, arraste ele aqui.")]
    [SerializeField] private SpriteRenderer lidSpriteRenderer;
    [Tooltip("Se você usa uma UI Image (Canvas) para o tampo do piano, arraste ela aqui.")]
    [SerializeField] private Image lidUIImage;
    [Tooltip("Sprite para tampo fechado (aplica a SpriteRenderer ou Image quando useSpriteForLid == true)")]
    [SerializeField] private Sprite lidClosedSprite;
    [Tooltip("Sprite para tampo aberto (aplica a SpriteRenderer ou Image quando useSpriteForLid == true)")]
    [SerializeField] private Sprite lidOpenSprite;
    
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
        
        // If using sprites and we don't have explicit sprites assigned, try to get them
        if (useSpriteForLid)
        {
            // If the user didn't assign a SpriteRenderer, check on this GameObject or children
            if (lidSpriteRenderer == null)
            {
                lidSpriteRenderer = GetComponent<SpriteRenderer>();
                if (lidSpriteRenderer == null)
                    lidSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            // Auto-assign lidOpenSprite from the sprite renderer's current sprite if not set
            if (lidOpenSprite == null && lidSpriteRenderer != null)
            {
                lidOpenSprite = lidSpriteRenderer.sprite;
            }

            // Auto-assign lidClosedSprite from pianoLidClosed if it's a SpriteRenderer or Image
            if (lidClosedSprite == null && pianoLidClosed != null)
            {
                var sr = pianoLidClosed.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    lidClosedSprite = sr.sprite;
                }
                else
                {
                    var img = pianoLidClosed.GetComponent<Image>();
                    if (img != null) lidClosedSprite = img.sprite;
                }
            }
        }
    }

    // Ensure changes in the Inspector update sprites and GameObjects immediately in the Editor
    private void OnValidate()
    {
        // Only update visuals in the Editor when not playing
        if (Application.isPlaying) return;

        // Validate configuration: if using sprite mode, warn when no sprite target assigned
        if (useSpriteForLid)
        {
            if (lidSpriteRenderer == null && lidUIImage == null)
            {
                Debug.LogWarning("PianoController: 'useSpriteForLid' is true but no 'lidSpriteRenderer' or 'lidUIImage' is assigned.");
            }
            if (lidClosedSprite == null || lidOpenSprite == null)
            {
                Debug.LogWarning("PianoController: 'useSpriteForLid' is true but 'lidClosedSprite' or 'lidOpenSprite' is not assigned. Visuals will not swap correctly.");
            }
            // Try to auto-fill sprites in editor for convenience
            if (lidSpriteRenderer == null)
            {
                lidSpriteRenderer = GetComponent<SpriteRenderer>();
                if (lidSpriteRenderer == null)
                    lidSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
            if (lidOpenSprite == null && lidSpriteRenderer != null)
            {
                lidOpenSprite = lidSpriteRenderer.sprite;
            }
            if (lidClosedSprite == null && pianoLidClosed != null)
            {
                var sr = pianoLidClosed.GetComponent<SpriteRenderer>();
                if (sr != null) lidClosedSprite = sr.sprite;
                else
                {
                    var img = pianoLidClosed.GetComponent<Image>();
                    if (img != null) lidClosedSprite = img.sprite;
                }
            }
        }

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
        // For sprite-based lids, update the sprite at start
        if (useSpriteForLid)
        {
            ApplyLidSprite();
        }
    }
    
    public override void Interact()
    {
        if (isPuzzleSolved)
        {
            // Se já resolvido, mostra uma mensagem
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.ShowDialogue("O piano já está fechado. As recompensas foram reveladas.");
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
        if (useSpriteForLid)
        {
            ApplyLidSprite();
            // Optionally hide the old objects if present so they don't overlay
            if (pianoLidClosed != null) pianoLidClosed.SetActive(false);
            if (pianoLidOpen != null) pianoLidOpen.SetActive(false);
            return;
        }

        if (pianoLidClosed != null)
        {
            pianoLidClosed.SetActive(!isPuzzleSolved);
        }
        
        if (pianoLidOpen != null)
        {
            pianoLidOpen.SetActive(isPuzzleSolved);
        }
    }

    private void ApplyLidSprite()
    {
        // If we have a SpriteRenderer (world object), set its sprite
        if (lidSpriteRenderer != null)
        {
            lidSpriteRenderer.sprite = isPuzzleSolved ? lidOpenSprite : lidClosedSprite;
            return;
        }

        // Otherwise, use UI Image
        if (lidUIImage != null)
        {
            lidUIImage.sprite = isPuzzleSolved ? lidOpenSprite : lidClosedSprite;
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
