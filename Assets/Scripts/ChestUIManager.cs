using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerencia o painel da UI do puzzle do baú trancado.
/// Suporta dois modos: campo de texto ou mostradores rotativos (dials).
/// Singleton persistente que deve estar no mesmo canvas do inventário.
/// </summary>
public class ChestUIManager : MonoBehaviour
{
    public static ChestUIManager Instance { get; private set; }

    // Note: input mode removed; currently only RotaryDials supported.

    [Header("Componentes da UI")]
    [Tooltip("Painel principal do puzzle do baú.")]
    [SerializeField] private GameObject chestPanel;

    [Header("Modo: Mostradores Rotativos")]
    [Tooltip("Array de mostradores (dials) para o modo rotativo.")]
    [SerializeField] private PasswordDial[] passwordDials;

    [Tooltip("Botão para fechar o painel.")]
    [SerializeField] private Button closeButton;

    [Header("UI Audio (botões)")]
    [Tooltip("Nome do SFX no SoundBank tocado quando um dial é alterado (opcional)")]
    [SerializeField] private string dialSfxName;
    [Tooltip("Start time (s) dentro do SFX para a fatia")]
    [SerializeField] private float dialSfxStart = 0f;
    [Tooltip("Duração (s) da fatia; se <=0, toca o clip inteiro")]
    [SerializeField] private float dialSfxDuration = 0f;
    [SerializeField] private AudioManager.Category dialSfxCategory = AudioManager.Category.UI;
    [Tooltip("Volume relativo do SFX do dial (0..1)")]
    [SerializeField] [Range(0f,1f)] private float dialSfxVolume = 1f;

    [Header("Configuração do Puzzle")]
    [Tooltip("A senha correta para abrir o baú. Defina isso no Inspector.")]
    [SerializeField] private string correctPassword = "1863";

    [Header("Áudio: Abertura do Baú")]
    [Tooltip("Nome do SFX no SoundBank tocado quando o baú abre (opcional)")]
    [SerializeField] private string chestOpenSfxName;
    [Tooltip("Start time (s) dentro do SFX para a fatia")]
    [SerializeField] private float chestOpenSfxStart = 0f;
    [Tooltip("Duração (s) da fatia; se <=0, toca o clip inteiro")]
    [SerializeField] private float chestOpenSfxDuration = 0f;
    [SerializeField] private AudioManager.Category chestOpenSfxCategory = AudioManager.Category.SFX;
    [Tooltip("Volume relativo do SFX do baú (0..1)")]
    [SerializeField] [Range(0f,1f)] private float chestOpenSfxVolume = 1f;
    [Tooltip("Delay (s) após tocar o SFX antes de chamar o controller para dar recompensas")]
    [SerializeField] private float chestOpenSolveDelay = 0.35f;

    private ChestController currentChest;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // keep this instance; do not return so initialization continues
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Configura os listeners dos botões
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePuzzle);
        }
    }

    private void Start()
    {
        if (chestPanel != null)
        {
            chestPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // nothing for now (feedback removed)
    }

    /// <summary>
    /// Abre o painel do puzzle do baú.
    /// </summary>
    public void OpenChestPuzzle(ChestController chest)
    {
        currentChest = chest;
        
        if (chestPanel != null)
        {
            chestPanel.SetActive(true);
        }
        // Reset dials
        if (passwordDials != null)
        {
            foreach (PasswordDial dial in passwordDials)
            {
                if (dial != null)
                {
                    dial.Reset();
                }
            }
        }
        // Block player input and pause game like other modal UIs
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.LockMovement();
        }
        UIInputBlocker.Block("ChestPuzzle");
        GamePauseManager.Pause("ChestPuzzle");

        // Ensure auto-close-on-cancel helper exists so Escape closes via our closeButton
        if (chestPanel != null && chestPanel.GetComponent<UIAutoCloseOnCancel>() == null)
        {
            var helper = chestPanel.AddComponent<UIAutoCloseOnCancel>();
            helper.panel = chestPanel;
            helper.closeButton = closeButton;
        }
        if (UnityEngine.EventSystems.EventSystem.current != null && closeButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
    }

    /// <summary>
    /// Fecha o painel do puzzle.
    /// </summary>
    public void ClosePuzzle()
    {
        if (chestPanel != null)
        {
            chestPanel.SetActive(false);
        }

        currentChest = null;

        // Restore player input and unpause
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.UnlockMovement();
        }
        UIInputBlocker.Unblock("ChestPuzzle");
        GamePauseManager.Unpause("ChestPuzzle");
    }

    /// <summary>
    /// Chamado quando o jogador pressiona Enter no input field.
    /// </summary>
    /// <summary>
    /// Valida a senha inserida pelo jogador.
    /// </summary>
    public void CheckPassword()
    {
        if (currentChest == null)
        {
            Debug.LogError("ChestUIManager: currentChest está null! Certifique-se de que OpenChestPuzzle foi chamado corretamente.");
            return;
        }

        string enteredPassword = "";
        // Concatena os valores dos dials
        if (passwordDials == null || passwordDials.Length == 0)
        {
            Debug.LogError("ChestUIManager: passwordDials não está configurado!");
            return;
        }

        foreach (PasswordDial dial in passwordDials)
        {
            if (dial != null)
            {
                enteredPassword += dial.GetValue().ToString();
            }
        }

        // Valida a senha
        if (enteredPassword.Equals(correctPassword, System.StringComparison.OrdinalIgnoreCase))
        {
            // Senha correta!
            
            // Som de sucesso (opcional)
            if (AudioManager.Instance != null)
            {
                // AudioManager.Instance.PlaySFX("chest_unlock");
            }
            
            // Guarda referência
            ChestController chest = currentChest;

            // Resolve o puzzle imediatamente (concede recompensas, aciona diário)
            if (chest != null)
            {
                chest.OnPuzzleSolved();
            }

            // Play chest open SFX and wait, then hide the UI (UI remains visible during SFX/delay)
            StartCoroutine(PlayChestOpenAndResolve());
        }
        else
        {
            // Senha incorreta: nenhum feedback visual por design (mantido silencioso)
        }
    }

    /// <summary>
    /// Mostra uma mensagem de feedback temporária.
    /// </summary>
    private void ShowFeedback(string message, Color color)
    {
        // feedback removed by design; intentionally left blank
    }

    /// <summary>
    /// Called by PasswordDial when a dial value changes.
    /// Triggers an immediate password check and plays the dial click SFX.
    /// </summary>
    public void OnDialValueChanged()
    {
        // Play click SFX via AudioManager using SoundBank name/slice
        if (!string.IsNullOrEmpty(dialSfxName) && AudioManager.Instance != null)
        {
            if (dialSfxDuration > 0f)
            {
                AudioManager.Instance.PlaySFXSlice(dialSfxName, dialSfxStart, dialSfxDuration, dialSfxVolume, dialSfxCategory);
            }
            else
            {
                AudioManager.Instance.PlaySFX(dialSfxName, dialSfxCategory, dialSfxVolume);
            }
        }

        // Auto-check password
        CheckPassword();
    }

    private System.Collections.IEnumerator PlayChestOpenAndResolve()
    {
        // Play SFX (non-spatial) via AudioManager using SoundBank name/slice
        if (!string.IsNullOrEmpty(chestOpenSfxName) && AudioManager.Instance != null)
        {
            if (chestOpenSfxDuration > 0f)
            {
                AudioManager.Instance.PlaySFXSlice(chestOpenSfxName, chestOpenSfxStart, chestOpenSfxDuration, chestOpenSfxVolume, chestOpenSfxCategory);
            }
            else
            {
                AudioManager.Instance.PlaySFX(chestOpenSfxName, chestOpenSfxCategory, chestOpenSfxVolume);
            }
        }

        // Wait same style as ClockUIManager (realtime) then hide the UI
        yield return new WaitForSecondsRealtime(chestOpenSolveDelay);

        ClosePuzzle();
    }

    /// <summary>
    /// Define a senha correta programaticamente (útil para testes ou configurações dinâmicas).
    /// </summary>
    public void SetCorrectPassword(string password)
    {
        correctPassword = password;
    }

    /// <summary>
    /// Retorna se o painel do puzzle está aberto.
    /// </summary>
    public bool IsPuzzleOpen()
    {
        return chestPanel != null && chestPanel.activeSelf;
    }
}
