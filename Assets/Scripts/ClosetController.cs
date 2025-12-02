using UnityEngine;

public class ClosetController : InteractableBase
{
    [Header("Configuração do Puzzle")]
    [Tooltip("Os 'moldes' dos itens (ScriptableObjects) que o jogador receberá.")]
    [SerializeField] private InventoryItem[] itemsToGive;
    
    [Tooltip("O diálogo que aparece quando o armário é inspecionado e já está vazio.")]
    [TextArea(2, 5)]
    [SerializeField] private string alreadyLootedDialogue;

    [Header("Controle de Estado")]
    [Tooltip("ID único para salvar o estado 'saqueado' do armário. Gere um clicando nos '...' do componente.")]
    [SerializeField] private string uniqueId;

    private bool isLooted = false;

    private void Start()
    {
        if (GameStateManager.Instance.IsCollected(uniqueId))
        {
            isLooted = true;
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    public override void OnInspect()
    {
        if (isLooted)
        {
            InteractionManager.Instance.ShowDialogue(alreadyLootedDialogue);
        }
        else
        {
            base.OnInspect();

            foreach (InventoryItem item in itemsToGive)
            {
                InventoryManager.Instance.AddItem(item);
            }

            isLooted = true;
            
            GameStateManager.Instance.MarkAsCollected(uniqueId);
        }
    }

    // Closet is inspect-only; don't show context menu, just inspect directly on interaction
    public override void Interact()
    {
        OnInspect();
    }

    public override bool CanShowContextMenu()
    {
        return false;
    }

    public override void OnUseItem(InventoryItem item)
    {
        base.OnUseItem(item);
    }
}
