using UnityEngine;

// Assumimos que PortraitData e InteractableBase já estão definidos
public class Portrait : InteractableBase
{
    [Header("Configuração de Dados")]
    [Tooltip("O modelo de dados que este retrato representa.")]
    public PortraitData data;

    // Referência ao controlador. Não precisa ser serializada.
    private PortraitPuzzleController puzzleController;

    protected override void Awake()
    {
        base.Awake();

    }

    private void Start()
    {
        // Encontra o controlador do puzzle usando o padrão Singleton (Instance)
        puzzleController = PortraitPuzzleController.Instance;

        // Log para garantir que o script está carregado
        Debug.Log("[Portrait] Trigger carregado e pronto para a interação.");

        if (puzzleController == null)
        {
            Debug.LogError("[Portrait] ERRO: PortraitPuzzleController (Instance) não foi encontrado.");
        }
    }
    
    
    public override void OnInspect()
    {
        // 1. Fecha o Context Menu (limpa a tela)
        InteractionManager.Instance.HideContextMenu();

        // 2. Verifica o Controller (Garanta que a busca por Singleton funcione)
        if (puzzleController == null)
        {
            // Se a busca no Start() falhou, tente buscar novamente agora.
            puzzleController = PortraitPuzzleController.Instance;
        }

        // 3. Abre a tela do puzzle
        if (puzzleController != null)
        {
            puzzleController.OpenPuzzleUI(data.portraitID);
        }
        else
        {
            Debug.LogError("ERRO FATAL: PortraitPuzzleController não foi encontrado! Não é possível abrir a UI.");
        }
    }
    
    
    }
    
    
    