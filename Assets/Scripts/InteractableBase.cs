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
    
    public virtual void OnInspect()
    {
        if (!string.IsNullOrEmpty(inspectionText))
        {
            InteractionManager.Instance.ShowDialogue(inspectionText);
        }
    }
    
    public virtual void OnUseItem(InventoryItem item)
    {
        Debug.Log($"O item '{item.itemName}' não funciona aqui.");
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

