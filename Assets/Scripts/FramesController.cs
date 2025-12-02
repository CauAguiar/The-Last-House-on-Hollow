using UnityEngine;

/// <summary>
/// Controller em cena para o puzzle de quadros (frames). Guarda as configurações (números iniciais
/// e sequência correta), persistência de estado e reação ao puzzle resolvido.
/// </summary>
public class FramesController : InteractableBase
{
    [Header("Configuração dos Quadros")]
    [Tooltip("Números iniciais a serem mostrados nos slots na mesma ordem dos filhos do slotsContainer.")]
    public int[] initialSlotNumbers;

    [Tooltip("Ordem correta de números para resolver o puzzle.")]
    public int[] correctOrderNumbers;

    [Header("Dica")] 
    // Removed diary page reward field: Frames only shows hint UI, no automatic journal reward.

    [Header("Estado")]
    [SerializeField] private string uniqueId;
    private bool isSolved = false;

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
        // Always open UI so player can view hint again if already solved
        FramesUIManager.Instance?.OpenPuzzle(this);
    }

    // Bypass context menu: open directly on interaction
    public override void Interact()
    {
        OnInspect();
    }

    public override bool CanShowContextMenu()
    {
        return false;
    }

    /// <summary>
    /// Called by the FramesUIManager when the puzzle is solved.
    /// Marks state and optionally grants journal page.
    /// </summary>
    public void OnPuzzleSolved()
    {
        if (isSolved) return;
        isSolved = true;
        GameStateManager.Instance.MarkAsCollected(uniqueId);
    }

    // Public getters used by the UI manager
    public int[] GetInitialSlotNumbers() => initialSlotNumbers;
    public int[] GetCorrectOrderNumbers() => correctOrderNumbers;
    public bool IsSolved() => isSolved;
}
