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
    [SerializeField] private Color proximityHighlightColor = new Color(1f, 1f, 1f, 0.75f); // Um branco semi-transparente
    [SerializeField] private float hoverScaleFactor = 1.1f;
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

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        originalScale = transform.localScale;
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
            // Fallback para quando o InteractionManager não estiver pronto
            Debug.Log(reply);
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
    }

    protected virtual void OnMouseEnter()
    {
        // O efeito de hover só acontece se o jogador já estiver perto.
        if (isPlayerNearby)
        {
            transform.localScale = originalScale * hoverScaleFactor;
        }
    }

    protected virtual void OnMouseExit()
    {
        transform.localScale = originalScale;
    }
}

