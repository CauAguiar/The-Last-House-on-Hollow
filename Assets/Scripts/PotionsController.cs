using UnityEngine;

public class PotionsController : InteractableBase
{
    [Header("Recompensas")]
    [SerializeField] private InventoryItem bedroomKey; // Chave Quarto Casal
    [Tooltip("ID numérico da página do diário a ser adicionada (corresponde ao JournalData.pages[].pageId)")]
    [SerializeField] private int diaryPage3Id = 3; // Página 3 (ID)

    [Header("Estado")]
    [SerializeField] private string uniqueId;
    private bool isSolved = false;
    [Header("World Visuals")]
    [Tooltip("Sprite a ser usado no mundo quando o puzzle estiver resolvido (ex: 'pocoes_certas').")]
    [SerializeField] private Sprite solvedSprite;

    private void Start()
    {
        isSolved = GameStateManager.Instance.IsCollected(uniqueId);
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    public override void OnInspect()
    {
        if (isSolved)
        {
            base.OnInspect(); // "As poções estão organizadas."
        }
        else
        {
            PotionsUIManager.Instance.OpenPuzzle(this);
        }
    }

    public void OnPuzzleSolved()
    {
        isSolved = true;
        GameStateManager.Instance.MarkAsCollected(uniqueId);

        // Dá os itens
        InventoryManager.Instance.AddItem(bedroomKey);
        // Marca a página 3 como coletada no Journal
        if (JournalManager.Instance != null)
        {
            JournalManager.Instance.CollectPage(diaryPage3Id);
        }
        else
        {
            // JournalManager.Instance is null: não foi possível adicionar a página do diário. (log removed)
        }

        // Atualiza sprite do objeto no mundo para estado resolvido (se configurado)
        if (solvedSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = solvedSprite;
        }

        InteractionManager.Instance.ShowDialogue("Um compartimento embaixo do armário abriu, que porra é essa? Tem uma <color=#5140ce>Chave</color> e uma <color=#5140ce>página do diário</color>.");
    }

    /// <summary>
    /// Atualiza os sprites visuais das poções no mundo com base na ordem atual do UI puzzle.
    /// </summary>
    public void UpdateWorldSprites(System.Collections.Generic.List<Sprite> spritesInOrder)
    {
        // Deprecated: replaced by single solvedSprite behavior. Keep method for compatibility but no-op.
        return;
    }
}