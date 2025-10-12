using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    [Header("Informacoes do Item ")]
    public string itemName = "Novo Item";
    [TextArea(3, 10)]
    public string description;
    public Sprite icon = null;

}
