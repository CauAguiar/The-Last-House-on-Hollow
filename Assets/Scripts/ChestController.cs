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

    private bool isOpened = false;

    protected override void Awake()
    {
        base.Awake();
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

    /// <summary>
    /// Chamado pelo ChestUIManager quando a senha correta é inserida.
    /// </summary>
    public void OnPuzzleSolved()
    {
        Debug.Log("Baú destrancado!");
        isOpened = true;
        GameStateManager.Instance.MarkAsCollected(uniqueId);
        
        // Muda o sprite para o baú aberto
        if (spriteRenderer != null && openSprite != null)
        {
            spriteRenderer.sprite = openSprite;
        }

        // Adiciona os itens ao inventário
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

        // Adiciona as páginas do diário
        if (diaryPageIds != null && diaryPageIds.Length > 0)
        {
            foreach (int pageId in diaryPageIds)
            {
                JournalManager.Instance.AddPage(pageId);
            }
        }

        // Mostra mensagem de sucesso
        string itemsText = "";
        if (rewardItems != null && rewardItems.Length > 0)
        {
            itemsText += "Encontrei: ";
            for (int i = 0; i < rewardItems.Length; i++)
            {
                if (rewardItems[i] != null)
                {
                    itemsText += $"<color=#fef08a>{rewardItems[i].itemName}</color>";
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
                itemsText += "Encontrei: ";
            }
            
            if (diaryPageIds.Length == 1)
            {
                itemsText += $"<color=#fef08a>Página {diaryPageIds[0]} do Diário</color>";
            }
            else
            {
                itemsText += $"<color=#fef08a>{diaryPageIds.Length} Páginas do Diário</color>";
            }
        }

        if (!string.IsNullOrEmpty(itemsText))
        {
            InteractionManager.Instance.ShowDialogue($"O baú se abre com um clique satisfatório! {itemsText}.");
        }
        else
        {
            InteractionManager.Instance.ShowDialogue("O baú se abre com um clique satisfatório!");
        }
    }
}
