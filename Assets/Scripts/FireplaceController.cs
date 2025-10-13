using UnityEditor.EditorTools;
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
            Debug.Log("Voce usa a vela apagada no fogo e ela se acende");
            InventoryManager.Instance.RemoveItem(unlitCandle);
            InventoryManager.Instance.AddItem(litCandle);

        } else
        {
            base.OnUseItem(item);
        }
    }
}
