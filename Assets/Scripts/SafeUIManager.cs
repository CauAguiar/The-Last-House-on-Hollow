using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI manager para o puzzle do cofre com numpad.
/// Exibe um display de 5 dígitos e botões numéricos 0-9 + confirmar + limpar/backspace.
/// Toca SFX de botão e SFX de sucesso quando a senha estiver correta.
/// </summary>
public class SafeUIManager : MonoBehaviour
{
    public static SafeUIManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private GameObject safePanel;
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private Button closeButton;

    [Header("Buttons (optional)")]
    [Tooltip("Se quiser conectar via código, atribua aqui os botões numéricos (0..9) na ordem ou use o método público OnNumericButtonClicked.")]
    [SerializeField] private Button[] numericButtons; // 0..9
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button backspaceButton;

    [Header("Audio (button)")]
    [SerializeField] private string buttonSfxName;
    [SerializeField] private float buttonSfxStart = 0f;
    [SerializeField] private float buttonSfxDuration = 0f;
    [SerializeField] private AudioManager.Category buttonSfxCategory = AudioManager.Category.UI;
    [SerializeField] [Range(0f,1f)] private float buttonSfxVolume = 1f;

    [Header("Audio: success")]
    [SerializeField] private string successSfxName;
    [SerializeField] private float successSfxStart = 0f;
    [SerializeField] private float successSfxDuration = 0f;
    [SerializeField] private AudioManager.Category successSfxCategory = AudioManager.Category.SFX;
    [SerializeField] [Range(0f,1f)] private float successSfxVolume = 1f;
    [SerializeField] private float successDelay = 0.35f;

    [Header("Puzzle")]
    [Tooltip("Número máximo de dígitos (defina 5 para esse puzzle)")]
    [SerializeField] private int passwordLength = 5;
    [Tooltip("Senha correta (ex.: " + "00000" + "). Defina no Inspector.")]
    [SerializeField] private string correctPassword = "12345";

    private SafeController currentSafe;
    private string currentInput = "";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        if (safePanel != null) safePanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePuzzle);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (backspaceButton != null) backspaceButton.onClick.AddListener(OnBackspaceClicked);

        if (numericButtons != null && numericButtons.Length >= 10)
        {
            // assign listeners if not already set in inspector
            for (int i = 0; i < 10; i++)
            {
                int captured = i;
                if (numericButtons[i] != null)
                {
                    numericButtons[i].onClick.RemoveAllListeners();
                    numericButtons[i].onClick.AddListener(() => OnNumericButtonClicked(captured));
                }
            }
        }

        UpdateDisplay();
    }

    public void OpenSafePuzzle(SafeController safe)
    {
        currentSafe = safe;
        currentInput = "";
        UpdateDisplay();

        if (safePanel != null) safePanel.SetActive(true);
        UIInputBlocker.Block("SafePuzzle");
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.LockMovement();
        GamePauseManager.Pause("SafePuzzle");

        if (safePanel != null && safePanel.GetComponent<UIAutoCloseOnCancel>() == null)
        {
            var helper = safePanel.AddComponent<UIAutoCloseOnCancel>();
            helper.panel = safePanel;
            helper.closeButton = closeButton;
        }

        if (UnityEngine.EventSystems.EventSystem.current != null && closeButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
    }

    public void ClosePuzzle()
    {
        if (safePanel != null) safePanel.SetActive(false);
        currentSafe = null;
        currentInput = "";
        UpdateDisplay();

        UIInputBlocker.Unblock("SafePuzzle");
        GamePauseManager.Unpause("SafePuzzle");
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.UnlockMovement();
    }

    private void UpdateDisplay()
    {
        if (displayText == null) return;
        string masked = currentInput;
        // Show underscore cursor if not full
        while (masked.Length < passwordLength) masked += "_";
        displayText.text = masked;
    }

    public void OnNumericButtonClicked(int digit)
    {
        if (currentInput.Length >= passwordLength) return;
        currentInput += digit.ToString();
        UpdateDisplay();
        PlayButtonSfx();
    }

    public void OnBackspaceClicked()
    {
        if (currentInput.Length == 0) return;
        currentInput = currentInput.Substring(0, currentInput.Length - 1);
        UpdateDisplay();
        PlayButtonSfx();
    }

    public void OnConfirmClicked()
    {
        PlayButtonSfx();
        if (currentInput.Length != passwordLength) return;

        if (currentInput.Equals(correctPassword))
        {
            // success
            if (!string.IsNullOrEmpty(successSfxName) && AudioManager.Instance != null)
            {
                if (successSfxDuration > 0f)
                    AudioManager.Instance.PlaySFXSlice(successSfxName, successSfxStart, successSfxDuration, successSfxVolume, successSfxCategory);
                else
                    AudioManager.Instance.PlaySFX(successSfxName, successSfxCategory, successSfxVolume);
            }

            // Notify controller and wait to close
            if (currentSafe != null)
            {
                currentSafe.OnPuzzleSolved();
            }

            StartCoroutine(PlaySuccessAndClose());
        }
        else
        {
            // incorrect: clear input
            currentInput = "";
            UpdateDisplay();
            // optional: play fail SFX (not provided)
        }
    }

    private IEnumerator PlaySuccessAndClose()
    {
        yield return new WaitForSecondsRealtime(successDelay);
        ClosePuzzle();
    }

    private void PlayButtonSfx()
    {
        if (AudioManager.Instance == null || string.IsNullOrEmpty(buttonSfxName)) return;
        if (buttonSfxDuration > 0f)
            AudioManager.Instance.PlaySFXSlice(buttonSfxName, buttonSfxStart, buttonSfxDuration, buttonSfxVolume, buttonSfxCategory);
        else
            AudioManager.Instance.PlaySFX(buttonSfxName, buttonSfxCategory, buttonSfxVolume);
    }

    // For debugging / testing
    public void SetCorrectPassword(string password)
    {
        correctPassword = password;
    }

    public bool IsOpen()
    {
        return safePanel != null && safePanel.activeSelf;
    }
}
