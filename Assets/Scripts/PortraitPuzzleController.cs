using System.Collections.Generic;
using UnityEngine;

public class PortraitPuzzleController : MonoBehaviour
{
    public static PortraitPuzzleController Instance; // Singleton para fácil acesso.

    [Header("Configuração")]
    public List<PortraitData> allPortraits; 
    public List<string> correctOrder; 
    
    [Header("UI do Puzzle")]
    public GameObject PuzzlePanel; // O painel Canvas que contém o puzzle
    public Transform portraitContainerUI; // O Transform Pai onde os retratos da UI ficarão
    public GameObject portraitUIPrefab; // O Prefab do item de UI de retrato (Imagem/Botão)

    [Header("Recompensa")]
    public GameObject rewardObject; 
    
    // Estado do puzzle na UI
    private PortraitUIPiece firstSelection = null;
    private bool puzzleSolved = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        PuzzlePanel.SetActive(false); // Esconde a UI no início
        if (rewardObject != null) rewardObject.SetActive(false);
    }
    
    // ------------------------------------------------------------------
    // LÓGICA DE INTERAÇÃO (Chamado pelo Portrait.cs)
    // ------------------------------------------------------------------
    
    public void OpenPuzzleUI(string clickedPortraitID)
    {
        // [Controller] Recebeu chamada para Abrir UI! (log removed)

        if (puzzleSolved) 
        {
            // [Controller] Puzzle já resolvido, ignorando abertura. (log removed)
            return;
        }

        if (PuzzlePanel != null)
        {
            PuzzlePanel.SetActive(true);
            PopulatePuzzleUI();
        }
        else
        {
            Debug.LogError("[Controller] ERRO: A referência 'Puzzle UI' no Inspector está faltando!");
        }
        
    }
        
    public void ClosePuzzleUI()
    {
       // PuzzlePanel.SetActive(false);

        PuzzlePanel.SetActive(false);

    }
    
    // ------------------------------------------------------------------
    // LÓGICA DE SWAP E VERIFICAÇÃO (Chamado pelo PortraitUIPiece.cs)
    // ------------------------------------------------------------------
    
    public void OnUIPieceSelected(PortraitUIPiece piece)
    {
        if (puzzleSolved) return; 

        // [Controller] Recebeu seleção (log removed)
        
        if (firstSelection == null)
        {
            // 1. Primeira seleção
            firstSelection = piece;
            // [Controller] Peça 1 Selecionada (log removed)
            return;
        }

        if (firstSelection == piece)
        {
            // 2. Clica no mesmo: cancela
            // [Controller] Seleção cancelada (log removed)
            firstSelection = null;
            return;
        }

        // 3. Segunda seleção: Troca
        // [Controller] Peça 2 Selecionada. Realizando Troca. (log removed)
        SwapUIPieces(firstSelection, piece);
        firstSelection = null; 
        
        CheckSolution();
    }

    private void SwapUIPieces(PortraitUIPiece p1, PortraitUIPiece p2)
    {
        // [Controller] SWAP: Trocando (log removed)
        
        // Troca APENAS a ordem na hierarquia (Visual na UI)
        int index1 = p1.transform.GetSiblingIndex();
        int index2 = p2.transform.GetSiblingIndex();

        p1.transform.SetSiblingIndex(index2);
        p2.transform.SetSiblingIndex(index1);
    }
    
    [ContextMenu("TESTE: Popular UI")]
    private void PopulatePuzzleUI()
    {
        // Limpa filhos antigos
        foreach (Transform child in portraitContainerUI)
        {
            Destroy(child.gameObject);
        }

        List<PortraitData> randomizedList = ShuffleList(allPortraits); 
        
        foreach (var data in randomizedList)
        {
            GameObject go = Instantiate(portraitUIPrefab, portraitContainerUI);
            PortraitUIPiece piece = go.GetComponent<PortraitUIPiece>();
            
            if (piece != null)
            {
                piece.Initialize(data); // Preenche a imagem e ID
            }
        }
    }

    private List<PortraitData> ShuffleList(List<PortraitData> list)
    {
        // Cria uma nova lista para ser embaralhada (para não alterar a lista original 'allPortraits')
        List<PortraitData> shuffled = new List<PortraitData>(list); 
        
        int n = shuffled.Count;
        while (n > 1)
        {
            n--;
            // Gera um índice aleatório entre 0 e n (incluindo n)
            int k = Random.Range(0, n + 1); 
            
            // Troca o elemento na posição k com o elemento na posição n
            PortraitData value = shuffled[k];
            shuffled[k] = shuffled[n];
            shuffled[n] = value;
        }
        return shuffled;
    }
    
    private void CheckSolution()
    {
        
        for (int i = 0; i < correctOrder.Count; i++)
        {
            PortraitUIPiece piece = portraitContainerUI.GetChild(i).GetComponent<PortraitUIPiece>();
            
            // Se o ID do retrato na posição 'i' não for o ID esperado
            if (piece == null || piece.data.portraitID != correctOrder[i])
            {
                // Falhou
                return; 
            }
        }
        
        // Se o loop terminar, a ordem está correta!
        PuzzleSolved();
    }

    private void PuzzleSolved()
    {
        puzzleSolved = true;
        // [PortraitPuzzle] SOLUÇÃO ENCONTRADA! (log removed)
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("PuzzleSolved"); 
            // [PortraitPuzzle] Som de Solução Tocado! (log removed)
        }
        else
        {
            // [PortraitPuzzle] AudioManager não encontrado. Impossível tocar o som! (log removed)
        }
        // Recompensa
        if (rewardObject != null) rewardObject.SetActive(true);
        
        ClosePuzzleUI();
    }
}