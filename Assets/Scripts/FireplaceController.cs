using UnityEngine;


public class FireplaceController : InteractableBase {

    [Header("Configuracao de itens")]
    [Tooltip("Asset de item vela apagada")]
    [SerializeField] private InventoryItem unlitCandle;

    [Tooltip("Asset de vela acesa")]
    [SerializeField] private InventoryItem litCandle;

    public override void OnUseItem (InventoryItem item)
    {
        if (item == unlitCandle)
        {
            InventoryManager.Instance.RemoveItem(unlitCandle);
            InteractionManager.Instance.ShowDialogue("Consegui acender a vela");
            InventoryManager.Instance.AddItem(litCandle);

        } else
        {
            base.OnUseItem(item);
        }
    }
}
