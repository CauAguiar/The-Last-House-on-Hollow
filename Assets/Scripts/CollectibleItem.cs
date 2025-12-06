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
    [Tooltip("Cor usada quando o jogador está próximo (proximity tint).")]
    [SerializeField] private Color highlightColor = new Color(0.3176471f, 0.2509804f, 0.8078431f, 1f); // #5140CE
    [SerializeField] private float hoverScaleFactor = 1.1f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale;
    private bool isHighlightedForProximity = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        originalScale = transform.localScale;
        // Nothing else to initialize for visual feedback; we tint the main SpriteRenderer on proximity.
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
        isHighlightedForProximity = true;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlightColor;
        }
    }

    public void OnProximityExit()
    {
        isHighlightedForProximity = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void OnMouseEnter()
    {
        // Hover enter
        // Só aplica o efeito de hover se o script estiver ativo
        if(this.enabled) 
        {
            transform.localScale = originalScale * hoverScaleFactor;
            // Opcional: muda a cor para branco para um destaque maior durante o hover
            spriteRenderer.color = Color.white;
        }
    }

    private void OnMouseExit()
    {
        // Hover exit
        // Só aplica o efeito de hover se o script estiver ativo
        if(this.enabled)
        {
            transform.localScale = originalScale;
            // Se o jogador ainda estiver perto, volta para a cor de proximidade, senão, para a original.
            spriteRenderer.color = isHighlightedForProximity ? highlightColor : originalColor;
        }
    }

    private void Update()
    {
        // nothing needed here for simple tint-based highlight
    }
}

