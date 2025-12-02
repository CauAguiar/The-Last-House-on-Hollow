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
    [SerializeField] private Color proximityHighlightColor = new Color(0.7019608f, 0f, 0.1490196f, 1f); // #B30026, opaco
    [SerializeField] private float hoverScaleFactor = 1.5f;
    [Header("Highlight Overlay")]
    [Tooltip("Se verdadeiro, usa um SpriteRenderer filho para desenhar um destaque/contorno ao invés de alterar a cor principal.")]
    [SerializeField] private bool useHighlightOverlay = true;
    [Tooltip("Cor do destaque (usada no SpriteRenderer filho)")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 1f);
    [Tooltip("Escala local do SpriteRenderer de destaque (multiplicador sobre a escala do objeto)")]
    [SerializeField] [Range(1.01f, 1.5f)] private float highlightScale = 1.08f;
    [Tooltip("Offset de sortingOrder aplicado ao highlight (negativo -> atrás, positivo -> na frente)")]
    [SerializeField] private int highlightSortingOrderOffset = -1;
    [Tooltip("Material usado para o highlight. Deve utilizar a máscara alpha do sprite para desenhar uma cor sólida (opcional). Se vazio, tentaremos usar o shader 'Custom/SpriteAlphaColor' se presente.")]
    [SerializeField] private Material highlightMaterial = null;
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
    // SpriteRenderer usado para o destaque/contorno; criado em Awake quando useHighlightOverlay == true
    private SpriteRenderer highlightRenderer;
    // Guarda o último sprite sincronizado para evitar atualizações desnecessárias
    private Sprite lastSyncedSprite;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        // Cria SpriteRenderer filho para destaque/contorno se solicitado
        if (useHighlightOverlay && spriteRenderer != null)
        {
            var go = new GameObject(gameObject.name + "_Highlight");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * highlightScale;
            highlightRenderer = go.AddComponent<SpriteRenderer>();
            highlightRenderer.sprite = spriteRenderer.sprite;
            // Assign material: use explicit material if provided, otherwise try to create one from shader
            if (highlightMaterial != null)
            {
                var mat = new Material(highlightMaterial);
                // Start hidden (alpha 0) and store visible color in highlightColor
                mat.SetColor("_Color", new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f));
                highlightRenderer.material = mat;
            }
            else
            {
                var sh = Shader.Find("Custom/SpriteAlphaColor");
                if (sh != null)
                {
                    var mat = new Material(sh);
                    mat.SetColor("_Color", new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f));
                    highlightRenderer.material = mat;
                }
                else
                {
                    // Fallback: just tint the sprite (won't produce a pure-color silhouette)
                    highlightRenderer.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
                }
            }
            highlightRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            highlightRenderer.sortingOrder = spriteRenderer.sortingOrder + highlightSortingOrderOffset;
            highlightRenderer.maskInteraction = SpriteMaskInteraction.None;
            highlightRenderer.gameObject.SetActive(true);
            lastSyncedSprite = spriteRenderer.sprite;
        }
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
        if (useHighlightOverlay && highlightRenderer != null)
        {
            // torna o overlay visível com a cor configurada
            if (highlightRenderer.material != null && highlightRenderer.material.HasProperty("_Color"))
            {
                var c = new Color(highlightColor.r, highlightColor.g, highlightColor.b, highlightColor.a);
                highlightRenderer.material.SetColor("_Color", c);
            }
            else
            {
                highlightRenderer.color = highlightColor;
            }
            highlightRenderer.transform.localScale = Vector3.one * highlightScale;
        }
        else
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = proximityHighlightColor;
            }
        }
    }

    public void OnProximityExit()
    {
        isPlayerNearby = false;
        if (useHighlightOverlay && highlightRenderer != null)
        {
            // Esconde o overlay (faz transparente)
            if (highlightRenderer.material != null && highlightRenderer.material.HasProperty("_Color"))
            {
                var c = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
                highlightRenderer.material.SetColor("_Color", c);
            }
            else
            {
                highlightRenderer.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
            }
        }
        else
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
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
        // Sincroniza sprite do highlight se o sprite principal mudar em runtime
        if (useHighlightOverlay && highlightRenderer != null && spriteRenderer != null)
        {
            var s = spriteRenderer.sprite;
            if (s != lastSyncedSprite)
            {
                highlightRenderer.sprite = s;
                lastSyncedSprite = s;
            }
        }
    }
}

