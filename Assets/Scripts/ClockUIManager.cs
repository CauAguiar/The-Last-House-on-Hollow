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
    private bool isSolving = false; // evita fechar repetidamente
    [SerializeField] private float solveDelay = 0.35f; // espera para feedback (som/clique) antes de fechar

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
        PlayerMovement.Instance.LockMovement();
        UIInputBlocker.Block("ClockPuzzle");
        GamePauseManager.Pause("ClockPuzzle");
        if (clockPanel != null && clockPanel.GetComponent<UIAutoCloseOnCancel>() == null)
        {
            var helper = clockPanel.AddComponent<UIAutoCloseOnCancel>();
            helper.panel = clockPanel;
            helper.closeButton = closeButton;
        }
        if (UnityEngine.EventSystems.EventSystem.current != null && closeButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
        // Opcional: Pausar o jogo aqui
    }

    public void ClosePuzzle()
    {
        clockPanel.SetActive(false);
        currentClock = null;
        PlayerMovement.Instance.UnlockMovement();
        UIInputBlocker.Unblock("ClockPuzzle");
        GamePauseManager.Unpause("ClockPuzzle");
        // Opcional: Despausar o jogo aqui
    }

    /// <summary>
    /// Chamado por CADA PONTEIRO toda vez que ele se move.
    /// Verifica se a hora está correta.
    /// </summary>
    public void CheckSolution() // <-- TORNADO PÚBLICO
    {
        // Se o puzzle já foi resolvido, não faz nada.
        if (currentClock == null || isSolving) return; 

        int currentHour = hourHand.currentValue; // 1..12
        int currentMinuteIndex = minuteHand.currentValue; // 1..12 (cada passo = 5 minutos)
        int currentMinutes = (currentMinuteIndex % 12) * 5;
        if (currentMinutes == 0) currentMinutes = 60; // 12 -> 60

        // Interpretação robusta: se solutionMinute estiver entre 1..12, tratamos como índice (casas)
        // Caso esteja >12, tratamos como minutos reais (múltiplos de 5).
        bool minuteMatch = (solutionMinute >= 1 && solutionMinute <= 12)
            ? (currentMinuteIndex == solutionMinute)
            : (currentMinutes == solutionMinute);

        bool solved = (currentHour == solutionHour && minuteMatch);

            // Debug detalhado (construído em partes para evitar erros de escape)
            string expectedMinutesStr;
            if (solutionMinute >= 1 && solutionMinute <= 12)
            {
                expectedMinutesStr = (solutionMinute * 5).ToString() + " (índice " + solutionMinute + ")";
            }
            else
            {
                expectedMinutesStr = solutionMinute.ToString();
            }
            string matchStr = solved ? "OK" : "NO";
            Debug.Log("[Clock Puzzle] Check: HoraAtual=" + currentHour +
                      " | MinIndexAtual=" + currentMinuteIndex +
                      " -> MinutosAtuais=" + currentMinutes +
                      " | EsperadoHora=" + solutionHour +
                      " | EsperadoMin=" + expectedMinutesStr +
                      " | Match=" + matchStr);
        if (solved)
        {
            // Sucesso! Dispara sequência com pequeno atraso para permitir feedback (som)
            Debug.Log($"[Clock Puzzle] Solvido! Hora={currentHour} Minutos={currentMinutes} (ÍndiceMinuto={currentMinuteIndex})");
            StartCoroutine(SolveSequence());
        }
    }

    private System.Collections.IEnumerator SolveSequence()
    {
        isSolving = true;
        // Toca SFX de puzzle resolvido (configurado no SoundBank)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ClockSolved");
        }
        yield return new WaitForSecondsRealtime(solveDelay);
        if (currentClock != null)
        {
            currentClock.OnPuzzleSolved();
        }
        ClosePuzzle();
        isSolving = false;
    }

    // Dentro de ClockUIManager
}