using UnityEngine;

/// <summary>
/// Controla o objeto da Máquina de Escrever na cena.
/// Herda de InteractableBase para funcionar com o sistema de clique.
/// </summary>
public class TypewriterController : InteractableBase
{
    [Header("Configuração do Puzzle")]
    [Tooltip("O poema que será exibido após a solução.")]
    [TextArea(5, 10)]
    [SerializeField] private string poemReward;

    [Header("Controle de Estado")]
    [Tooltip("ID único para salvar o estado 'resolvido'. Gere um clicando nos '...' do componente.")]
    [SerializeField] private string uniqueId;

    private bool isSolved = false;

    /// <summary>
    /// Expose o estado resolvido para usar por outros controladores (UIManager)
    /// </summary>
    public bool IsSolved => isSolved;

    private void Start()
    {
        // Verifica no GameStateManager se este puzzle já foi resolvido.
        isSolved = GameStateManager.Instance.IsCollected(uniqueId);
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Sobrescreve o método de inspeção.
    /// Chamado quando o jogador clica em "Inspecionar" no menu de contexto.
    /// </summary>
    public override void OnInspect()
    {
        // Sempre abre a UI do puzzle — a UI decidirá se deve exigir a senha
        // ou apenas mostrar o poema quando já estiver resolvido.
        TypewriterUIManager.Instance.OpenPuzzle(this, poemReward);
    }

    // Interact should open the UI directly (bypass context menu) and allow access even after solved
    public override void Interact()
    {
        TypewriterUIManager.Instance.OpenPuzzle(this, poemReward);
    }

    public override bool CanShowContextMenu()
    {
        return false;
    }

    /// <summary>
    /// Chamado pelo TypewriterUIManager quando o jogador digita a senha correta.
    /// </summary>
    public void OnPuzzleSolved()
    {
        isSolved = true;
        GameStateManager.Instance.MarkAsCollected(uniqueId);
        
        // (O diálogo de sucesso agora é mostrado pelo UIManager)
    }

    /// <summary>
    /// Garante que nenhum item possa ser usado na máquina de escrever.
    /// </summary>
    public override void OnUseItem(InventoryItem item)
    {
        // Apenas chama a lógica padrão (ex: "Isso não funciona aqui")
        base.OnUseItem(item);
    }
}