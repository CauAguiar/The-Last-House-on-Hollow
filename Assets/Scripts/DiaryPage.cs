using UnityEngine;

public class DiaryPage : InteractableBase
{
    [Header("Configuração da Página")]
    [Tooltip("O ID numérico desta página, que deve corresponder ao ID no JournalData.")]
    [SerializeField] private int pageId;

    [Tooltip("ID único para o GameStateManager salvar que esta página foi coletada. Gere um novo nos '...' do componente.")]
    [SerializeField] private string uniqueId;

    private void Start()
    {
        if (GameStateManager.Instance.IsCollected(uniqueId))
        {
            Destroy(gameObject);
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    public override void OnInspect()
    {
        base.OnInspect();
        JournalManager.Instance.CollectPage(pageId);
        GameStateManager.Instance.MarkAsCollected(uniqueId);
        Destroy(gameObject);
    }

    public override void OnUseItem(InventoryItem item)
    {
        base.OnUseItem(item);
    }
}
