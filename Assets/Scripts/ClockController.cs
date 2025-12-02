using UnityEngine;

public class ClockController : InteractableBase
{
    [Header("Configuração do Puzzle")]
    [Tooltip("Item de recompensa ao resolver (ex: Pé de Cabra). Se vazio, nenhum item será dado.")]
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

    // Bypass context menu: interact should open inspect/UI directly
    public override void Interact()
    {
        OnInspect();
    }

    public override bool CanShowContextMenu()
    {
        return false;
    }

    public void OnPuzzleSolved()
    {
        isSolved = true;
        GameStateManager.Instance.MarkAsCollected(uniqueId);
        spriteRenderer.sprite = openSprite;
        // Give reward item if configured and not already owned
        if (crowbarItem != null && InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.HasItem(crowbarItem))
            {
                InventoryManager.Instance.AddItem(crowbarItem);
            }
        }

        if (crowbarItem != null)
        {
            InteractionManager.Instance.ShowDialogue("Com um 'clique', um compartimento se abre. Encontrei um <color=#fef08a>" + crowbarItem.itemName + "</color>!");
        }
        else
        {
            InteractionManager.Instance.ShowDialogue("Com um 'clique', um compartimento se abre.");
        }
    }
}
