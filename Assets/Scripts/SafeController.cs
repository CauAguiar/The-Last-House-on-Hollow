using UnityEngine;

/// <summary>
/// Interactable que representa o cofre. Ao inspecionar, abre o `SafeUIManager`.
/// Quando a senha correta for inserida, concede um item, desbloqueia a última página
/// do diário e habilita o Tomo.
/// </summary>
public class SafeController : InteractableBase
{
    [Header("Recompensas")]
    [Tooltip("Item que será dado ao jogador ao abrir o cofre (ex: chave)")]
    [SerializeField] private InventoryItem rewardItem;

    [Tooltip("ID da página do diário que será concedida (última página)")]
    [SerializeField] private int diaryPageId = 0;

    [Tooltip("Sprite do cofre aberto (opcional)")]
    [SerializeField] private Sprite openSprite;

    [Header("Estado/Saving")]
    [Tooltip("ID único usado para marcar o cofre como resolvido")]
    [SerializeField] private string uniqueId;

    [Header("Tomo")]
    [Tooltip("Se verdadeiro, quando o cofre for aberto habilita o Tomo na UI de atalhos")]
    [SerializeField] private bool grantsTome = true;

    private bool isOpened = false;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null && GameStateManager.Instance.IsCollected(uniqueId))
        {
            isOpened = true;
            if (spriteRenderer != null && openSprite != null)
            {
                spriteRenderer.sprite = openSprite;
            }
        }
    }

    public override void OnInspect()
    {
        if (isOpened)
        {
            InteractionManager.Instance.ShowDialogue("O cofre está vazio.");
        }
        else
        {
            if (SafeUIManager.Instance != null)
            {
                SafeUIManager.Instance.OpenSafePuzzle(this);
            }
        }
    }

    public void OnPuzzleSolved()
    {
        // Called by UI manager when password is correct. Grant rewards and persist.
        isOpened = true;
        if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.MarkAsCollected(uniqueId);
        }

        if (spriteRenderer != null && openSprite != null)
        {
            spriteRenderer.sprite = openSprite;
        }

        if (rewardItem != null)
        {
            InventoryManager.Instance.AddItem(rewardItem);
        }

        if (diaryPageId > 0 && JournalManager.Instance != null)
        {
            JournalManager.Instance.CollectPage(diaryPageId);
        }

        if (grantsTome)
        {
            QuickAccessButtons.Instance?.SetTomeAvailable(true);
        }

        // Provide brief dialogue feedback
        InteractionManager.Instance.ShowDialogue("Você ouviu um clique metálico e algo caiu — uma chave e uma página do diário apareceram.");
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }
}
