using UnityEngine;

/// <summary>
/// Controla a lógica do puzzle do quadro da família Craven.
/// Herda de InteractableBase e sobrescreve seus métodos.
/// </summary>
public class PortraitController : InteractableBase
{
    [Header("Configuração do Puzzle")]
    [Tooltip("O 'asset' do item (ScriptableObject) do Pé de Cabra.")]
    [SerializeField] private InventoryItem crowbarItem;
    
    [Tooltip("O texto da pista que aparece depois de usar o pé de cabra.")]
    [TextArea(2, 5)]
    [SerializeField] private string clueText;

    [Tooltip("ID único para salvar o estado 'removido' do quadro.")]
    [SerializeField] private string uniqueId;

    private bool isRemoved = false;

    // A variável 'inspectionTexts' (array) é herdada de InteractableBase.
    // Preencha no Inspector com os seus dois diálogos.

    protected override void Awake()
    {
        base.Awake(); // Chama o Awake() da classe base (InteractableBase)
    }

    private void Start()
    {
        // Verifica no GameStateManager se este quadro já foi removido em uma sessão anterior.
        if (GameStateManager.Instance.IsCollected(uniqueId))
        {
            isRemoved = true;
        }
    }

    // Este atributo adiciona um botão no menu de contexto do componente no Inspector.
    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Sobrescreve a lógica de inspeção para lidar com os dois estados.
    /// </summary>
    public override void OnInspect()
    {
        if (isRemoved)
        {
            // Se o quadro já foi removido, mostra a pista.
            InteractionManager.Instance.ShowDialogue(clueText);
        }
        else
        {
            // Se ainda está preso, usa a lógica de diálogo sequencial da classe base.
            base.OnInspect();
        }
    }

    /// <summary>
    /// Sobrescreve a lógica de uso de item.
    /// </summary>
    public override void OnUseItem(InventoryItem item)
    {
        // Se o quadro já foi removido, nenhum item faz mais nada.
        if (isRemoved)
        {
            base.OnUseItem(item);
            return;
        }

        // Verifica se o item usado é o pé de cabra
        if (item == crowbarItem)
        {
            // Sucesso!
            isRemoved = true;
            GameStateManager.Instance.MarkAsCollected(uniqueId); // Salva o estado permanentemente
            
            // Mostra a pista imediatamente
            InteractionManager.Instance.ShowDialogue(clueText);
            
            // (Opcional: remover o pé de cabra se for de uso único)
            // InventoryManager.Instance.RemoveItem(crowbarItem);
        }
        else
        {
            // Se usou o item errado, chama a lógica padrão (ex: "Isso não funciona aqui")
            base.OnUseItem(item);
        }
    }
}
