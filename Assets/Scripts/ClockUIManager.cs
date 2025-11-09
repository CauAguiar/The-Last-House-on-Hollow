using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gerencia o painel da UI do puzzle do relógio.
/// </summary>
public class ClockUIManager : MonoBehaviour
{
    public static ClockUIManager Instance;

    [Header("Configuração da Solução")]
    [Tooltip("A hora correta (1-12).")]
    [SerializeField] private int solutionHour = 7;
    [Tooltip("O minuto correto (1-12, pois trava nos números).")]
    [SerializeField] private int solutionMinute = 5;

    [Header("Referências da UI")]
    [SerializeField] private GameObject clockPanel;
    [SerializeField] private ClockHand hourHand;
    [SerializeField] private ClockHand minuteHand;
    // [SerializeField] private Button submitButton; // <-- REMOVIDO
    [SerializeField] private Button closeButton; 

    private ClockController currentClock; 

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

        // submitButton.onClick.AddListener(CheckSolution); // <-- REMOVIDO
        closeButton.onClick.AddListener(ClosePuzzle);
    }

    private void Start()
    {
        clockPanel.SetActive(false); 
    }

    public void OpenClockPuzzle(ClockController clock)
    {
        currentClock = clock;
        clockPanel.SetActive(true);
        // Opcional: Pausar o jogo aqui
    }

    public void ClosePuzzle()
    {
        clockPanel.SetActive(false);
        currentClock = null;
        // Opcional: Despausar o jogo aqui
    }

    /// <summary>
    /// Chamado por CADA PONTEIRO toda vez que ele se move.
    /// Verifica se a hora está correta.
    /// </summary>
    public void CheckSolution() // <-- TORNADO PÚBLICO
    {
        // Se o puzzle já foi resolvido, não faz nada.
        if (currentClock == null) return; 

        int currentHour = hourHand.currentValue;
        int currentMinute = minuteHand.currentValue;

        if (currentHour == solutionHour && currentMinute == solutionMinute)
        {
            // Sucesso!
            currentClock.OnPuzzleSolved();
            ClosePuzzle(); // Fecha automaticamente ao acertar
        }
    }
}