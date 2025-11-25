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

    [Header("Recompensa / Dica")] 
    [Tooltip("ID de página do diário a desbloquear (opcional)")]
    public int diaryPageId = 0;

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

    /// <summary>
    /// Called by the FramesUIManager when the puzzle is solved.
    /// Marks state and optionally grants journal page.
    /// </summary>
    public void OnPuzzleSolved()
    {
        if (isSolved) return;
        isSolved = true;
        GameStateManager.Instance.MarkAsCollected(uniqueId);

        if (diaryPageId > 0 && JournalManager.Instance != null)
        {
            JournalManager.Instance.CollectPage(diaryPageId);
        }
    }

    // Public getters used by the UI manager
    public int[] GetInitialSlotNumbers() => initialSlotNumbers;
    public int[] GetCorrectOrderNumbers() => correctOrderNumbers;
    public bool IsSolved() => isSolved;
}
