using System.Collections.Generic;
using UnityEngine;

public class CandelabroController : MonoBehaviour
{
    public static CandelabroController Instance;

    [Header("Configuração do Puzzle")]
    // Ordem: 3, 1, 4, 2, 5
    public List<int> correctOrder = new List<int> { 3, 1, 4, 2, 5 }; 
    
    [Header("UI e Recompensa")]
    public GameObject closeUpPanel; 
    public GameObject rewardKey; 
    
    private List<int> playerSequence = new List<int>();
    private bool puzzleSolved = false;

    [Header("Testes e Setup")]
    public InventoryItem testVelaAcesa;

    void Start()
    {
        // Test setup removed for release; do not add test item at Start.
        
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }
    
    public void OpenCloseUpUI()
    {
        if (puzzleSolved) return;
        closeUpPanel.SetActive(true);
        playerSequence.Clear(); 
        // Adicionar o reset visual aqui depois
    }
    
    public void RegisterCandleClick(int candleID)
    {
        if (puzzleSolved) return;
        playerSequence.Add(candleID);
        if (playerSequence.Count == correctOrder.Count)
        {
           // CheckSequence();
        }
    }

    private void CheckSequence()
    {
        // 1. Compara a sequência do jogador com a sequência correta (3, 1, 4, 2, 5)
        for (int i = 0; i < correctOrder.Count; i++)
        {
            // Se alguma vela clicada na posição 'i' não for igual à ID esperada
            if (playerSequence[i] != correctOrder[i])
            {
                ResetPuzzle();
                return; // Sai da função, pois a sequência falhou
            }
        }

        // 2. Se o loop terminou sem falhas, a sequência está correta!
        PuzzleSolved();
    }

    private void ResetPuzzle()
    {
        playerSequence.Clear();

        // 1. Obtém todas as velas da UI (do container)
        Candle[] candles = closeUpPanel.GetComponentsInChildren<Candle>();

        // 2. Manda cada vela apagar visualmente
        foreach (Candle candle in candles)
        {
            candle.ResetVisual(); 
        }
    }
    
    private void PuzzleSolved()
    {
        puzzleSolved = true;
        closeUpPanel.SetActive(false);
        
        // 1. Toca o som de sucesso
        if (AudioManager.Instance != null)
        {
           
            AudioManager.Instance.PlaySFX("PuzzleSolved"); 
        }

        // 2. A chave do Banheiro aparece (RewardKey)
        if (rewardKey != null)
        {
            rewardKey.SetActive(true); // Faz a chave do banheiro aparecer
        }
    }
        

}