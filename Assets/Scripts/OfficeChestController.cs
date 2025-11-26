using UnityEngine;

/// <summary>
/// Controller para o baú do escritório. Abre a UI de letras (OfficeChestUIManager).
/// </summary>
public class OfficeChestController : InteractableBase
{
    [Header("Recompensas")]
    [SerializeField] private InventoryItem[] rewardItems;
    [SerializeField] private int[] diaryPageIds;
    [SerializeField] private Sprite openSprite;

    [Header("Estado/Persistência")]
    [SerializeField] private string uniqueId;

    private bool isOpened = false;

    private void Start()
    {
        if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance.IsCollected(uniqueId))
        {
            isOpened = true;
            if (spriteRenderer != null && openSprite != null) spriteRenderer.sprite = openSprite;
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    public override void OnInspect()
    {
        if (isOpened)
        {
            InteractionManager.Instance.ShowDialogue("O baú está vazio. Já peguei tudo que havia aqui.");
        }
        else
        {
            OfficeChestUIManager.Instance.OpenChestPuzzle(this);
        }
    }

    public void OnPuzzleSolved()
    {
        isOpened = true;
        if (!string.IsNullOrEmpty(uniqueId)) GameStateManager.Instance.MarkAsCollected(uniqueId);

        if (spriteRenderer != null && openSprite != null) spriteRenderer.sprite = openSprite;

        if (rewardItems != null && rewardItems.Length > 0)
        {
            foreach (var it in rewardItems) if (it != null) InventoryManager.Instance.AddItem(it);
        }

        if (diaryPageIds != null && diaryPageIds.Length > 0)
        {
            foreach (int id in diaryPageIds) JournalManager.Instance.CollectPage(id);
        }

        // Feedback similar ao ChestController
        string itemsText = "";
        if (rewardItems != null && rewardItems.Length > 0)
        {
            itemsText += "Encontrei: ";
            for (int i = 0; i < rewardItems.Length; i++)
            {
                if (rewardItems[i] != null)
                {
                    itemsText += $"<color=#fef08a>{rewardItems[i].itemName}</color>";
                    if (i < rewardItems.Length - 1) itemsText += ", ";
                }
            }
        }

        if (diaryPageIds != null && diaryPageIds.Length > 0)
        {
            if (!string.IsNullOrEmpty(itemsText)) itemsText += " e "; else itemsText += "Encontrei: ";
            if (diaryPageIds.Length == 1) itemsText += $"<color=#fef08a>Página {diaryPageIds[0]} do Diário</color>";
            else itemsText += $"<color=#fef08a>{diaryPageIds.Length} Páginas do Diário</color>";
        }

        if (!string.IsNullOrEmpty(itemsText)) InteractionManager.Instance.ShowDialogue($"O baú do escritório se abre! {itemsText}.");
        else InteractionManager.Instance.ShowDialogue("O baú do escritório se abre!");
    }
}
