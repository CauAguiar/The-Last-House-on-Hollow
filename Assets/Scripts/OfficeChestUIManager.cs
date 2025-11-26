using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Manager para o puzzle do baú do escritório (letras em vez de números).
/// Reusa a mesma UI de dials, mas cada dial exibe letras.
/// </summary>
public class OfficeChestUIManager : MonoBehaviour
{
    public static OfficeChestUIManager Instance { get; private set; }

    [Header("Componentes da UI")]
    [SerializeField] private GameObject chestPanel;

    [Header("Dials (letras)")]
    [SerializeField] private OfficePasswordDial[] letterDials;

    [Tooltip("Botão de fechar painel")]
    [SerializeField] private Button closeButton;

    [Header("SFX do Dial")]
    [SerializeField] private string dialSfxName;
    [SerializeField] private float dialSfxStart = 0f;
    [SerializeField] private float dialSfxDuration = 0f;
    [SerializeField] private AudioManager.Category dialSfxCategory = AudioManager.Category.UI;
    [SerializeField] [Range(0f,1f)] private float dialSfxVolume = 1f;

    [Header("Configuração do Puzzle")]
    [Tooltip("Senha correta (ex: BETA). Deve ter o mesmo comprimento que o número de dials.")]
    [SerializeField] private string correctPassword = "BETA";

    [Header("SFX Abertura")]
    [SerializeField] private string chestOpenSfxName;
    [SerializeField] private float chestOpenSfxStart = 0f;
    [SerializeField] private float chestOpenSfxDuration = 0f;
    [SerializeField] private AudioManager.Category chestOpenSfxCategory = AudioManager.Category.SFX;
    [SerializeField] [Range(0f,1f)] private float chestOpenSfxVolume = 1f;
    [SerializeField] private float chestOpenSolveDelay = 0.35f;

    private OfficeChestController currentChest;

    private void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }

        if (closeButton != null) closeButton.onClick.AddListener(ClosePuzzle);
    }

    private void Start()
    {
        if (chestPanel != null) chestPanel.SetActive(false);
    }

    public void OpenChestPuzzle(OfficeChestController chest)
    {
        currentChest = chest;
        if (chestPanel != null) chestPanel.SetActive(true);

        // Configure dials letters if needed (default letters A..J preserved)
        if (letterDials != null)
        {
            foreach (var d in letterDials) d?.Reset();
        }

        if (PlayerMovement.Instance != null) PlayerMovement.Instance.LockMovement();
        UIInputBlocker.Block("OfficeChestPuzzle");
        GamePauseManager.Pause("OfficeChestPuzzle");

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

    public void ClosePuzzle()
    {
        if (chestPanel != null) chestPanel.SetActive(false);
        currentChest = null;
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.UnlockMovement();
        UIInputBlocker.Unblock("OfficeChestPuzzle");
        GamePauseManager.Unpause("OfficeChestPuzzle");
    }

    /// <summary>
    /// Chamado quando qualquer dial muda de valor.
    /// </summary>
    public void OnDialValueChanged()
    {
        // Play dial SFX
        if (!string.IsNullOrEmpty(dialSfxName) && AudioManager.Instance != null)
        {
            if (dialSfxDuration > 0f)
                AudioManager.Instance.PlaySFXSlice(dialSfxName, dialSfxStart, dialSfxDuration, dialSfxVolume, dialSfxCategory);
            else
                AudioManager.Instance.PlaySFX(dialSfxName, dialSfxCategory, dialSfxVolume);
        }

        // Auto-check
        CheckPassword();
    }

    public void CheckPassword()
    {
        if (currentChest == null)
        {
            Debug.LogWarning("OfficeChestUIManager: currentChest null");
            return;
        }

        if (letterDials == null || letterDials.Length == 0)
        {
            Debug.LogWarning("OfficeChestUIManager: letterDials não configurado");
            return;
        }

        string entered = "";
        foreach (var d in letterDials)
        {
            if (d != null) entered += d.GetChar();
        }

        if (entered.Equals(correctPassword, System.StringComparison.OrdinalIgnoreCase))
        {
            // solved
            var chest = currentChest;
            if (chest != null) chest.OnPuzzleSolved();
            StartCoroutine(PlayChestOpenAndResolve());
        }
        else
        {
            // incorrect: silent by design
        }
    }

    private System.Collections.IEnumerator PlayChestOpenAndResolve()
    {
        if (!string.IsNullOrEmpty(chestOpenSfxName) && AudioManager.Instance != null)
        {
            if (chestOpenSfxDuration > 0f)
                AudioManager.Instance.PlaySFXSlice(chestOpenSfxName, chestOpenSfxStart, chestOpenSfxDuration, chestOpenSfxVolume, chestOpenSfxCategory);
            else
                AudioManager.Instance.PlaySFX(chestOpenSfxName, chestOpenSfxCategory, chestOpenSfxVolume);
        }

        yield return new WaitForSecondsRealtime(chestOpenSolveDelay);
        ClosePuzzle();
    }

    public void SetCorrectPassword(string password)
    {
        correctPassword = password;
    }

    public bool IsPuzzleOpen() { return chestPanel != null && chestPanel.activeSelf; }
}
