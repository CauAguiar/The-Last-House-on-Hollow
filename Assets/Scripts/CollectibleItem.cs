using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class CollectibleItem : MonoBehaviour, IInteractable
{
    [Header("Item Data")]
    [Tooltip("O 'molde' do item (ScriptableObject) que será adicionado ao inventário.")]
    [SerializeField] private InventoryItem itemData;

    [Header("Identificação Única")]
    [Tooltip("ID único para este item na cena. Clique nos três pontinhos (...) do componente e selecione 'Generate Unique ID' para criar um.")]
    [SerializeField] private string uniqueId;

    [Header("Configurações de Feedback Visual")]
    [Tooltip("Se verdadeiro, usa um SpriteRenderer filho para desenhar um destaque/contorno ao invés de alterar a cor principal.")]
    [SerializeField] private bool useHighlightOverlay = true;
    [Tooltip("Cor do destaque (usada no SpriteRenderer filho)")]
    [SerializeField] private Color highlightColor = new Color(0.7019608f, 0f, 0.1490196f, 1f);
    [Tooltip("Escala local do SpriteRenderer de destaque (multiplicador sobre a escala do objeto)")]
    [SerializeField] [Range(1.01f, 1.5f)] private float highlightScale = 1.08f;
    [Tooltip("Offset de sortingOrder aplicado ao highlight (negativo -> atrás, positivo -> na frente)")]
    // Default offset set to +1 so highlight renders above the main sprite by default
    [SerializeField] private int highlightSortingOrderOffset = 1;
    [Tooltip("Material usado para o highlight. Deve utilizar a máscara alpha do sprite para desenhar uma cor sólida (opcional). Se vazio, usaremos tint do SpriteRenderer.")]
    [SerializeField] private Material highlightMaterial = null;
    [SerializeField] private float hoverScaleFactor = 1.1f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale;
    private bool isHighlightedForProximity = false;
    // Optional overlay renderer used for a solid-color highlight similar to InteractableBase
    private SpriteRenderer highlightRenderer;
    private Sprite lastSyncedSprite;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        originalScale = transform.localScale;
        // Create highlight overlay renderer if requested
        if (useHighlightOverlay && spriteRenderer != null)
        {
            var go = new GameObject(gameObject.name + "_Highlight");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * highlightScale;
            highlightRenderer = go.AddComponent<SpriteRenderer>();
            highlightRenderer.sprite = spriteRenderer.sprite;
            // Assign material: use explicit material if provided, otherwise fallback to tint
            if (highlightMaterial != null)
            {
                var mat = new Material(highlightMaterial);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f));
                highlightRenderer.material = mat;
            }
            else
            {
                // start fully transparent
                highlightRenderer.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
            }
            highlightRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            highlightRenderer.sortingOrder = spriteRenderer.sortingOrder + highlightSortingOrderOffset;
            highlightRenderer.maskInteraction = SpriteMaskInteraction.None;
            highlightRenderer.gameObject.SetActive(true);
            // Ensure highlight uses same flip/scale as base sprite
            highlightRenderer.flipX = spriteRenderer.flipX;
            highlightRenderer.flipY = spriteRenderer.flipY;
            Debug.Log($"CollectibleItem: created highlightRenderer for '{gameObject.name}' (sortingOrder={highlightRenderer.sortingOrder}, sprite={(highlightRenderer.sprite!=null?highlightRenderer.sprite.name:"(null)")})");
            lastSyncedSprite = spriteRenderer.sprite;
            Debug.Log($"CollectibleItem: created highlightRenderer for '{gameObject.name}' (useHighlightOverlay={useHighlightOverlay}, hasMaterial={highlightMaterial != null})");
        }
    }

    private void Start()
    {
        // Se o GameManager já sabe que este item foi coletado, destrói-o imediatamente.
        if (GameStateManager.Instance.IsCollected(uniqueId))
        {
            Destroy(gameObject);
        }
    }

    // Este atributo adiciona um botão no menu de contexto do componente no Inspector.
    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    public void Interact()
    {
        if (itemData == null)
        {
            Debug.LogError($"Item coletável '{gameObject.name}' não tem um ItemData associado!");
            return;
        }

        // 1. Avisa ao GameManager que este item foi coletado.
        GameStateManager.Instance.MarkAsCollected(uniqueId);
        
        // 2. Adiciona ao inventário.
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(itemData);
            }
        else
        {
            Debug.LogError("CollectibleItem: InventoryManager.Instance não encontrado. Item não foi adicionado ao inventário.");
        }

        // 3. Destrói o objeto.
        Destroy(gameObject);
    }

    // --- Métodos de Feedback Visual ---

    public void OnProximityEnter()
    {
        Debug.Log($"CollectibleItem: OnProximityEnter('{gameObject.name}'), highlightRenderer={(highlightRenderer!=null)}");
        isHighlightedForProximity = true;
        if (useHighlightOverlay && highlightRenderer != null)
        {
            var c = new Color(highlightColor.r, highlightColor.g, highlightColor.b, highlightColor.a);
            if (highlightRenderer.material != null && highlightRenderer.material.HasProperty("_Color"))
            {
                highlightRenderer.material.SetColor("_Color", c);
            }
            // always set tint so SpriteRenderer.color reflects desired alpha
            highlightRenderer.color = c;
            highlightRenderer.transform.localScale = Vector3.one * highlightScale;
            // read material color if available for accurate debug
            string matColorInfo = "";
            if (highlightRenderer.material != null && highlightRenderer.material.HasProperty("_Color"))
            {
                var mc = highlightRenderer.material.GetColor("_Color");
                matColorInfo = $", materialColor.a={mc.a}";
            }
            Debug.Log($"CollectibleItem: OnProximityEnter setting highlight color alpha={highlightRenderer.color.a}{matColorInfo}, sortingOrder={highlightRenderer.sortingOrder}, sprite={(highlightRenderer.sprite!=null?highlightRenderer.sprite.name:"(null)")}");
        }
        else
        {
            spriteRenderer.color = highlightColor;
        }
    }

    public void OnProximityExit()
    {
        Debug.Log($"CollectibleItem: OnProximityExit('{gameObject.name}'), highlightRenderer={(highlightRenderer!=null)}");
        isHighlightedForProximity = false;
        if (useHighlightOverlay && highlightRenderer != null)
        {
            var c = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
            if (highlightRenderer.material != null && highlightRenderer.material.HasProperty("_Color"))
            {
                highlightRenderer.material.SetColor("_Color", c);
            }
            highlightRenderer.color = c;
            Debug.Log($"CollectibleItem: OnProximityExit hiding highlight (alpha={highlightRenderer.color.a})");
        }
        else
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void OnMouseEnter()
    {
        Debug.Log($"CollectibleItem: OnMouseEnter('{gameObject.name}'), highlightRenderer={(highlightRenderer!=null)}");
        // Só aplica o efeito de hover se o script estiver ativo
        if(this.enabled) 
        {
            transform.localScale = originalScale * hoverScaleFactor;
            // Opcional: muda a cor para branco para um destaque maior durante o hover
            if (useHighlightOverlay && highlightRenderer != null)
            {
                var c = new Color(1f, 1f, 1f, 1f);
                if (highlightRenderer.material != null && highlightRenderer.material.HasProperty("_Color"))
                {
                    highlightRenderer.material.SetColor("_Color", c);
                }
                highlightRenderer.color = c;
                Debug.Log($"CollectibleItem: OnMouseEnter set highlight white (alpha={highlightRenderer.color.a})");
            }
            else
            {
                spriteRenderer.color = Color.white;
            }
        }
    }

    private void OnMouseExit()
    {
        Debug.Log($"CollectibleItem: OnMouseExit('{gameObject.name}'), highlightRenderer={(highlightRenderer!=null)} isHighlightedForProximity={isHighlightedForProximity}");
        // Só aplica o efeito de hover se o script estiver ativo
        if(this.enabled)
        {
            transform.localScale = originalScale;
            // Se o jogador ainda estiver perto, volta para a cor de proximidade, senão, para a original.
            if (useHighlightOverlay && highlightRenderer != null)
            {
                if (isHighlightedForProximity)
                {
                    var c = new Color(highlightColor.r, highlightColor.g, highlightColor.b, highlightColor.a);
                    if (highlightRenderer.material != null && highlightRenderer.material.HasProperty("_Color"))
                    {
                        highlightRenderer.material.SetColor("_Color", c);
                    }
                    highlightRenderer.color = c;
                }
                else
                {
                    var c = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
                    if (highlightRenderer.material != null && highlightRenderer.material.HasProperty("_Color"))
                    {
                        highlightRenderer.material.SetColor("_Color", c);
                    }
                    highlightRenderer.color = c;
                }
                Debug.Log($"CollectibleItem: OnMouseExit updated highlight (alpha={highlightRenderer.color.a}, sortingOrder={highlightRenderer.sortingOrder})");
            }
            else
            {
                spriteRenderer.color = isHighlightedForProximity ? highlightColor : originalColor;
            }
        }
    }

    private void Update()
    {
        // Sync sprite if main sprite changes at runtime
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

