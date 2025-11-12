using UnityEngine;

public class CandelabroTrigger : InteractableBase 
{
    [Tooltip("O item que o jogador precisa ter para interagir (Vela Acesa).")]
    public InventoryItem requiredItem; 

    private CandelabroController controller;
    
    protected override void Awake() 
    {
        base.Awake();
    }

    private void Start()
    {
        
        controller = CandelabroController.Instance;

        if (controller == null)
        {
            Debug.LogError("[CandelabroTrigger] CandelabroController.Instance não encontrado. O puzzle não funcionará.");
        }
    }
    
    [ContextMenu("TESTE: Abrir Puzzle")]
    public void TestOpenPuzzle()
    {
        // Chama a lógica de sucesso (OnInspect) para abrir o puzzle.
        this.OnInspect();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica se o objeto que entrou é o Player (baseado no tag ou nome)
        if (other.gameObject.CompareTag("Player")) 
        {
            Debug.LogWarning("DETECÇÃO POR PROXIMIDADE FUNCIONOU! Agora o menu deve abrir.");
            
            // A lógica de abertura de menu
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.ShowContextMenu(this);
            }
        }
    }
    
    public override void OnInspect()
    {
        // 2. Garante que o InventoryManager exista antes de checar HasItem
        if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredItem))
        {
            // Item Correto: Abre o puzzle
            InteractionManager.Instance.HideContextMenu();
            
            if (controller != null)
            {
                controller.OpenCloseUpUI();
            }
            else
            {
                 Debug.LogError("[CandelabroTrigger] Controller nulo no OnInspect!");
            }
        }
        else
        {
            // Item Ausente: Mostra a pista (diálogo)
            InteractionManager.Instance.ShowDialogue("As velas estão apagadas. Não consigo acendê-las com as mãos...");
        }
    }
    
    public override void OnUseItem(InventoryItem item)
    {
        // O OnUseItem é deixado para mensagens padrão ou combinação de itens.
        InteractionManager.Instance.ShowDialogue("Este candelabro precisa ser aceso, não removido.");
    }
}