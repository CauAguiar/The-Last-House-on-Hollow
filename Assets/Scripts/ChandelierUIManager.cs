using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI manager para o puzzle do candelabro (velas). Gerencia 5 botões (velas), checa inventário
/// antes de permitir acender, controla sequência, reseta em caso de falha e dá recompensa ao resolver.
/// </summary>
public class ChandelierUIManager : MonoBehaviour
{
    public static ChandelierUIManager Instance;

    [Header("Referências da UI")]
    [SerializeField] private GameObject puzzlePanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private CandleButton[] candleButtons; // assign 5 in inspector

    [Header("Feedback")]
    [SerializeField] private string needCandleMessage = "Você precisa de uma vela acesa no inventário para acender as velas.";
    [Header("SFX e Delay")]
    [Tooltip("Nome do SFX tocado ao resolver o puzzle (opcional)")]
    [SerializeField] private string successSfxName;
    [Tooltip("Start time (s) dentro do SFX para a fatia")]
    [SerializeField] private float successSfxStart = 0f;
    [Tooltip("Duração (s) da fatia; se <=0, toca o clip inteiro")]
    [SerializeField] private float successSfxDuration = 0f;
    [SerializeField] private AudioManager.Category successSfxCategory = AudioManager.Category.SFX;
    [Tooltip("Volume relativo do SFX de sucesso (0..1)")]
    [SerializeField] [Range(0f,1f)] private float successSfxVolume = 1f;
    [Tooltip("Delay (s) após tocar o SFX antes de fechar a UI")]
    [SerializeField] private float successCloseDelay = 0.35f;

    [Tooltip("Nome do SFX tocado ao errar a sequência (opcional)")]
    [SerializeField] private string failSfxName;
    [Tooltip("Start time (s) dentro do SFX para a fatia")]
    [SerializeField] private float failSfxStart = 0f;
    [Tooltip("Duração (s) da fatia; se <=0, toca o clip inteiro")]
    [SerializeField] private float failSfxDuration = 0f;
    [SerializeField] private AudioManager.Category failSfxCategory = AudioManager.Category.SFX;
    [Tooltip("Volume relativo do SFX de erro (0..1)")]
    [SerializeField] [Range(0f,1f)] private float failSfxVolume = 1f;

    [Header("Pulse Feedback")]
    [SerializeField] private float missingItemPulseScale = 1.15f;
    [SerializeField] private float missingItemPulseDuration = 0.45f;

    private List<int> sequence = new List<int>();
    private ChandelierController currentController;

    private void Awake()
    {
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    private void Start()
    {
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePuzzle);
    }

    public void OpenPuzzle(ChandelierController controller)
    {
        currentController = controller;
        if (puzzlePanel == null) return;

        puzzlePanel.SetActive(true);
        PlayerMovement.Instance?.LockMovement();
        UIInputBlocker.Block("Chandelier");
        GamePauseManager.Pause("Chandelier");

        // Reset UI state (do not change solved reveal here)
        sequence.Clear();
        foreach (var cb in candleButtons) cb?.SetLit(false);
    }

    public void ClosePuzzle()
    {
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
        currentController = null;
        sequence.Clear();
        PlayerMovement.Instance?.UnlockMovement();
        UIInputBlocker.Unblock("Chandelier");
        GamePauseManager.Unpause("Chandelier");
    }

    public void OnCandleClicked(int index)
    {
        if (currentController == null)
        {
            return;
        }

        // require inventory item to be able to light
        if (currentController.requiredLitCandle != null && InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.HasItem(currentController.requiredLitCandle))
            {
                // Visual feedback (pulse) on the clicked candle if available, otherwise pulse the panel
                if (index >= 0 && index < candleButtons.Length && candleButtons[index] != null)
                {
                    StartCoroutine(PulseTransform(candleButtons[index].transform, missingItemPulseScale, missingItemPulseDuration));
                }
                else if (puzzlePanel != null)
                {
                    StartCoroutine(PulseTransform(puzzlePanel.transform, missingItemPulseScale, missingItemPulseDuration));
                }

                InteractionManager.Instance.ShowDialogue(needCandleMessage);
                return;
            }
        }

        // light the clicked candle (if not already lit)
        if (index >= 0 && index < candleButtons.Length)
        {
            var btn = candleButtons[index];
            if (btn != null)
            {
                if (!btn.IsLit())
                {
                    btn.SetLit(true);
                    sequence.Add(index); // store 0-based
                }
                else
                {
                    // clicking an already-lit candle is treated as a mistake: reset immediately
                    StartCoroutine(HandleFailure("A porra do vento apagou as velas"));
                    return;
                }
            }
        }

        // if five candles lit, check sequence
        if (sequence.Count >= candleButtons.Length)
        {
            CheckSequence();
        }
    }

    /// <summary>
    /// Returns true if the current puzzle/controller allows lighting a candle (inventory requirement satisfied or none).
    /// Used by UI elements to decide whether to play click feedback before delegating logic to the manager.
    /// </summary>
    public bool CanLight(int index)
    {
        if (currentController == null) return true;
        if (currentController.requiredLitCandle == null) return true;
        if (InventoryManager.Instance == null) return false;
        return InventoryManager.Instance.HasItem(currentController.requiredLitCandle);
    }

    private void CheckSequence()
    {
        // expected sequence is 0..N-1 (here 0..4)
        bool correct = true;
        for (int i = 0; i < sequence.Count; i++)
        {
            if (sequence[i] != i) { correct = false; break; }
        }

        if (correct)
        {
            // success: notify controller immediately, play success SFX and close after delay
            if (currentController != null)
            {
                currentController.OnPuzzleSolved();
            }
            StartCoroutine(PlaySuccessAndClose());
        }
        else
        {
            // failure -> extinguish all and reset with fail SFX and feedback
            StartCoroutine(HandleFailure("A porra do vento apagou as velas"));
        }
    }

    private System.Collections.IEnumerator PlaySuccessAndClose()
    {
        // Play success SFX (slice support)
        if (!string.IsNullOrEmpty(successSfxName) && AudioManager.Instance != null)
        {
            if (successSfxDuration > 0f)
                AudioManager.Instance.PlaySFXSlice(successSfxName, successSfxStart, successSfxDuration, successSfxVolume, successSfxCategory);
            else
                AudioManager.Instance.PlaySFX(successSfxName, successSfxCategory, successSfxVolume);
        }

        yield return new WaitForSecondsRealtime(successCloseDelay);

        // Close UI (restore input)
        ClosePuzzle();
    }

    private System.Collections.IEnumerator HandleFailure(string message)
    {
        // Play fail SFX
        if (!string.IsNullOrEmpty(failSfxName) && AudioManager.Instance != null)
        {
            if (failSfxDuration > 0f)
                AudioManager.Instance.PlaySFXSlice(failSfxName, failSfxStart, failSfxDuration, failSfxVolume, failSfxCategory);
            else
                AudioManager.Instance.PlaySFX(failSfxName, failSfxCategory, failSfxVolume);
        }

        // extinguish visuals
        foreach (var cb in candleButtons) cb?.SetLit(false);
        sequence.Clear();

        // feedback dialogue
        if (!string.IsNullOrEmpty(message) && InteractionManager.Instance != null)
        {
            InteractionManager.Instance.ShowDialogue(message);
        }

        // short yield to allow SFX/visuals to be perceived
        yield return null;
    }

    private System.Collections.IEnumerator PulseTransform(Transform target, float targetScale, float duration)
    {
        if (target == null) yield break;
        Vector3 original = target.localScale;
        float half = duration * 0.5f;
        float elapsed = 0f;

        // scale up
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / half));
            target.localScale = Vector3.Lerp(original, original * targetScale, p);
            yield return null;
        }

        // scale down
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / half));
            target.localScale = Vector3.Lerp(original * targetScale, original, p);
            yield return null;
        }

        target.localScale = original;
    }
}
