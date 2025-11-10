using UnityEngine;

public class ClockController : InteractableBase
{
    [Header("Configuração do Puzzle")]
    [Tooltip("Item de recompensa ao resolver.")]
    [SerializeField] private InventoryItem crowbarItem;
    
    [Tooltip("Sprite do relógio após ser aberto.")]
    [SerializeField] private Sprite openSprite;

    [Header("Controle de Estado")]
    [Tooltip("ID único para salvar o estado 'resolvido'.")]
    [SerializeField] private string uniqueId;

    private bool isSolved = false;

    protected override void Awake()
    {
        base.Awake();
        // 'spriteRenderer' já é inicializado em InteractableBase.Awake()
    }

    private void Start()
    {
        if (GameStateManager.Instance.IsCollected(uniqueId))
        {
            isSolved = true;
            spriteRenderer.sprite = openSprite;
        }
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
            base.OnInspect();
        }
        else
        {
            ClockUIManager.Instance.OpenClockPuzzle(this);
        }
    }

    public void OnPuzzleSolved()
    {
        Debug.Log("Puzzle do Relógio Resolvido!");
        isSolved = true;
        GameStateManager.Instance.MarkAsCollected(uniqueId);
        spriteRenderer.sprite = openSprite;
        InventoryManager.Instance.AddItem(crowbarItem);
        InteractionManager.Instance.ShowDialogue("Com um 'clique', um compartimento se abre. Encontrei um <color=#fef08a>Pé de Cabra</color>!");
    }
}
