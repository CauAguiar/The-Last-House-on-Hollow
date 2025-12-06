using UnityEngine;

/// <summary>
/// Controlador do baú trancado no Salão Principal.
/// Requer uma senha para ser aberto, que é obtida após usar o pé de cabra no quadro.
/// </summary>
public class ChestController : InteractableBase
{
    [Header("Configuração do Puzzle")]
    [Tooltip("Itens que serão dados ao jogador quando o baú for aberto.")]
    [SerializeField] private InventoryItem[] rewardItems;
    
    [Tooltip("IDs das páginas do diário que serão desbloqueadas (ex: 1, 2, 3).")]
    [SerializeField] private int[] diaryPageIds;
    
    [Tooltip("Sprite do baú aberto.")]
    [SerializeField] private Sprite openSprite;

    [Header("Controle de Estado")]
    [Tooltip("ID único para salvar o estado 'aberto' do baú.")]
    [SerializeField] private string uniqueId;

    // Audio and delay are handled by the Chest UI manager (moved to ChestUIManager)

    private bool isOpened = false;

    protected override void Awake()
    {
        base.Awake();
        // Chest has no special audio initialization; UI manages SFX and delay.
    }

    private void Start()
    {
        // Verifica se o baú já foi aberto anteriormente
        if (GameStateManager.Instance.IsCollected(uniqueId))
        {
            isOpened = true;
            if (spriteRenderer != null && openSprite != null)
            {
                spriteRenderer.sprite = openSprite;
            }
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
            // Se já está aberto, mostra mensagem
            InteractionManager.Instance.ShowDialogue("O baú está vazio. Já peguei tudo que havia aqui.");
        }
        else
        {
            // Abre a UI do puzzle de senha
            ChestUIManager.Instance.OpenChestPuzzle(this);
        }
    }

    // Bypass the global context menu: this chest should open its UI directly on click
    public override void Interact()
    {
        OnInspect();
    }

    public override bool CanShowContextMenu()
    {
        return false;
    }

    /// <summary>
    /// Chamado pelo ChestUIManager quando a senha correta é inserida.
    /// </summary>
    public void OnPuzzleSolved()
    {
        // Chest resolved immediately by the controller (UI already handled SFX + delay)
        isOpened = true;
        GameStateManager.Instance.MarkAsCollected(uniqueId);

        // Change sprite
        if (spriteRenderer != null && openSprite != null)
        {
            spriteRenderer.sprite = openSprite;
        }

        // Give items
        if (rewardItems != null && rewardItems.Length > 0)
        {
            foreach (InventoryItem item in rewardItems)
            {
                if (item != null)
                {
                    InventoryManager.Instance.AddItem(item);
                }
            }
        }

        // Collect diary pages (use CollectPage to trigger UI notifications)
        if (diaryPageIds != null && diaryPageIds.Length > 0)
        {
            foreach (int pageId in diaryPageIds)
            {
                JournalManager.Instance.CollectPage(pageId);
            }
        }

        // Show success dialogue
        string itemsText = "";
        if (rewardItems != null && rewardItems.Length > 0)
        {
            itemsText += "encontrei uma ";
            for (int i = 0; i < rewardItems.Length; i++)
            {
                if (rewardItems[i] != null)
                {
                    itemsText += $"<color=#5140ce>{rewardItems[i].itemName}</color>";
                    if (i < rewardItems.Length - 1)
                    {
                        itemsText += ", ";
                    }
                }
            }
        }

        if (diaryPageIds != null && diaryPageIds.Length > 0)
        {
            if (!string.IsNullOrEmpty(itemsText))
            {
                itemsText += " e ";
            }
            else
            {
                itemsText += "encontrei: ";
            }
            if (diaryPageIds.Length == 1)
            {
                itemsText += $"<color=#5140ce>Página {diaryPageIds[0]} do Diário</color>";
            }
            else
            {
                itemsText += $"<color=#5140ce>{diaryPageIds.Length} Páginas do Diário</color>";
            }
        }

        if (!string.IsNullOrEmpty(itemsText))
        {
            InteractionManager.Instance.ShowDialogue($"Ta só o pó, mas  {itemsText}.");
        }
        else
        {
            InteractionManager.Instance.ShowDialogue("Ta só o pó, mas ");
        }
    }
}
