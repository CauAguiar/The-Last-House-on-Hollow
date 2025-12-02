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
        if (pageId == 0)
        {
            // DiaryPage has pageId=0 — please set the correct pageId in the Inspector. (log removed)
        }
    }

    [Header("Recompensa de Inventário (opcional)")]
    [Tooltip("Se atribuído, este InventoryItem será adicionado ao inventário quando a página for coletada.")]
    [SerializeField] private InventoryItem inventoryReward;

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

        // Se houver um item de recompensa configurado, adiciona ao inventário (se ainda não estiver presente)
        if (inventoryReward != null)
        {
            if (InventoryManager.Instance != null)
            {
                if (!InventoryManager.Instance.HasItem(inventoryReward))
                {
                    InventoryManager.Instance.AddItem(inventoryReward);
                }
            }
            else
            {
                // DiaryPage: InventoryManager.Instance é null — não foi possível adicionar o inventoryReward da página. (log removed)
            }
        }

        Destroy(gameObject);
    }

    // Diary pages are collect-only: clicking should collect and show inspection text, no context menu
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
