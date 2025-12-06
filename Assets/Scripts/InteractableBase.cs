using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Base class for all interactable objects, providing default visual feedback logic.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Configuração de Interação")]
    [TextArea(2, 5)]
    public string inspectionText;

    [Header("Feedback Visual")]
    [SerializeField] private Color proximityHighlightColor = new Color(0.3176471f, 0.2509804f, 0.8078431f, 1f); // #5140CE
    [SerializeField] private float hoverScaleFactor = 1.5f;
    [Header("Cursor")]
    [Tooltip("Cursor a ser exibido quando o mouse estiver sobre o objeto e o jogador estiver próximo. Use uma textura pequena (ex: 32x32) com transparência.")]
    [SerializeField] private Texture2D cursorHand = null;
    [Tooltip("Hotspot (offset) do cursor em pixels. Geralmente (0,0) ou centro da imagem.")]
    [SerializeField] private Vector2 cursorHotspot = new Vector2(0, 0);
    // Expose hover scale so proximity logic can consider it when deciding "nearby"
    public float HoverScaleFactor => hoverScaleFactor;

    [Header("Respostas para uso incorreto")]
    [Tooltip("Frases que serão mostradas aleatoriamente quando o jogador tentar usar um item que não funciona aqui.")]
    [SerializeField]
    private string[] wrongItemResponses = new string[]
    {
        "Não acho que deva usar isso aqui.",
        "Devo estar endoidando usando isso aqui.",
        "Acho que isso não vai funcionar aqui.",
        "Não parece o lugar certo para isso.",
        "Essa merda não funciona aqui."
    };

    protected SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale;
    private bool isPlayerNearby = false;
    // (no overlay) keep visuals simple by tinting the main SpriteRenderer

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        // No overlay creation: visual feedback is applied by tinting the main SpriteRenderer.
        originalScale = transform.localScale;
        // Ensure a Collider2D exists - some objects may have lost it and Unity will warn about RequiredComponent
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            // Add a BoxCollider2D as a safe default and mark as trigger so it doesn't affect physics
            var added = gameObject.AddComponent<BoxCollider2D>();
            added.isTrigger = true;
        }
        // Ensure there is a global InteractableDefaults instance in the scene for default cursor settings
        if (InteractableDefaults.Instance == null)
        {
            var go = new GameObject("InteractableDefaults");
            go.AddComponent<InteractableDefaults>();
        }
    }

    public virtual void Interact()
    {
        InteractionManager.Instance.ShowContextMenu(this);
    }

    /// <summary>
    /// Hook used by InteractionManager to determine whether the context menu
    /// should be shown for this interactable. Override to customize behavior
    /// (e.g., doors that are already unlocked should not show the menu).
    /// </summary>
    public virtual bool CanShowContextMenu()
    {
        return true;
    }

    /// <summary>
    /// Returns true if the player is currently within proximity of this interactable.
    /// This is set by the proximity detection system (`PlayerInteractionController`).
    /// </summary>
    public bool IsPlayerInProximity()
    {
        return isPlayerNearby;
    }
    
    public virtual void OnInspect()
    {
        if (!string.IsNullOrEmpty(inspectionText))
        {
            InteractionManager.Instance.ShowDialogue(inspectionText);
        }
    }
    
    public virtual void OnUseItem(InventoryItem item)
    {
        if (item == null)
        {
            // Nada a fazer se nenhum item foi passado
            return;
        }

        string reply = "Não acho que deva usar isso aqui.";
        if (wrongItemResponses != null && wrongItemResponses.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, wrongItemResponses.Length);
            reply = wrongItemResponses[idx];
        }

        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.ShowDialogue(reply);
        }
        else
        {
            // Fallback para quando o InteractionManager não estiver pronto (silenciado)
        }
    }
    
    // --- Lógica de Feedback Visual ---

    public void OnProximityEnter()
    {
        isPlayerNearby = true;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = proximityHighlightColor;
        }
    }

    public void OnProximityExit()
    {
        isPlayerNearby = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        // Garante que a escala volte ao normal se o jogador se afastar enquanto o mouse está sobre o objeto
        transform.localScale = originalScale;
        // Se o cursor estava alterado, restaura para o padrão quando o jogador sai da proximidade
        var cursorToUse = cursorHand != null ? cursorHand : (InteractableDefaults.Instance != null ? InteractableDefaults.Instance.defaultCursor : null);
        if (cursorToUse != null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    protected virtual void OnMouseEnter()
    {
        // O efeito de hover só acontece se o jogador já estiver perto.
        if (isPlayerNearby)
        {
            transform.localScale = originalScale * hoverScaleFactor;
            // Muda o cursor para a mão: usa o cursor local se configurado, caso contrário usa o default global
            if (!UIInputBlocker.IsBlocked)
            {
                Texture2D toSet = cursorHand != null ? cursorHand : (InteractableDefaults.Instance != null ? InteractableDefaults.Instance.defaultCursor : null);
                Vector2 hs = cursorHand != null ? cursorHotspot : (InteractableDefaults.Instance != null ? InteractableDefaults.Instance.defaultHotspot : Vector2.zero);
                if (toSet != null)
                {
                    Cursor.SetCursor(toSet, hs, CursorMode.Auto);
                }
            }
        }
    }

    protected virtual void OnMouseExit()
    {
        transform.localScale = originalScale;
        // Restaura cursor ao sair do objeto
        var cursorToUse = cursorHand != null ? cursorHand : (InteractableDefaults.Instance != null ? InteractableDefaults.Instance.defaultCursor : null);
        if (cursorToUse != null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    private void Update()
    {
        // No runtime sync required for overlay-less tint approach
    }
}

