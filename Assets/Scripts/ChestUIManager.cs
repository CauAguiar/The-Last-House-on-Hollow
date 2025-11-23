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

    public enum PasswordInputMode
    {
        TextField,      // Modo tradicional com campo de texto
        RotaryDials     // Modo moderno com mostradores rotativos
    }

    [Header("Modo de Entrada")]
    [Tooltip("Escolha entre campo de texto ou mostradores rotativos.")]
    [SerializeField] private PasswordInputMode inputMode = PasswordInputMode.RotaryDials;

    [Header("Componentes da UI")]
    [Tooltip("Painel principal do puzzle do baú.")]
    [SerializeField] private GameObject chestPanel;

    [Header("Modo: Campo de Texto")]
    [Tooltip("Campo de texto onde o jogador digita a senha.")]
    [SerializeField] private TMP_InputField passwordInputField;

    [Header("Modo: Mostradores Rotativos")]
    [Tooltip("Array de mostradores (dials) para o modo rotativo.")]
    [SerializeField] private PasswordDial[] passwordDials;

    [Header("Botões")]
    [Tooltip("Botão para submeter a senha.")]
    [SerializeField] private Button submitButton;

    [Tooltip("Botão para fechar o painel.")]
    [SerializeField] private Button closeButton;

    [Header("Feedback")]
    [Tooltip("Texto de feedback para o jogador (sucesso/erro).")]
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Configuração do Puzzle")]
    [Tooltip("A senha correta para abrir o baú. Defina isso no Inspector.")]
    [SerializeField] private string correctPassword = "1863";

    [Tooltip("Mensagem quando a senha está incorreta.")]
    [SerializeField] private string wrongPasswordMessage = "A senha está incorreta...";

    [Tooltip("Tempo que a mensagem de feedback fica visível (em segundos).")]
    [SerializeField] private float feedbackDuration = 2f;

    private ChestController currentChest;
    private float feedbackTimer = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Garantir que seja raiz antes de marcar como persistente para evitar o warning
            if (transform.parent != null)
            {
                transform.SetParent(null); // torna este GameObject root
            }
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Configura os listeners dos botões
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(CheckPassword);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePuzzle);
        }

        // Configura o input field para submeter ao pressionar Enter
        if (passwordInputField != null)
        {
            passwordInputField.onSubmit.AddListener(OnPasswordSubmit);
        }
    }

    private void Start()
    {
        if (chestPanel != null)
        {
            chestPanel.SetActive(false);
        }

        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Gerencia o timer do feedback
        if (feedbackTimer > 0)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0 && feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }
        }
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

        // Limpa o estado baseado no modo
        if (inputMode == PasswordInputMode.TextField)
        {
            // Modo campo de texto
            if (passwordInputField != null)
            {
                passwordInputField.text = "";
                passwordInputField.Select();
                passwordInputField.ActivateInputField();
            }
        }
        else if (inputMode == PasswordInputMode.RotaryDials)
        {
            // Modo mostradores rotativos - reseta todos os dials para 0
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
        }

        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }

        feedbackTimer = 0f;
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
        feedbackTimer = 0f;
    }

    /// <summary>
    /// Chamado quando o jogador pressiona Enter no input field.
    /// </summary>
    private void OnPasswordSubmit(string password)
    {
        CheckPassword();
    }

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

        // Obtém a senha baseado no modo
        if (inputMode == PasswordInputMode.TextField)
        {
            if (passwordInputField == null)
            {
                Debug.LogError("ChestUIManager: passwordInputField está null!");
                return;
            }
            enteredPassword = passwordInputField.text.Trim();
        }
        else if (inputMode == PasswordInputMode.RotaryDials)
        {
            if (passwordDials == null || passwordDials.Length == 0)
            {
                Debug.LogError("ChestUIManager: passwordDials não está configurado!");
                return;
            }

            // Concatena os valores dos dials
            foreach (PasswordDial dial in passwordDials)
            {
                if (dial != null)
                {
                    enteredPassword += dial.GetValue().ToString();
                }
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
            
            // Guarda referência antes de fechar o painel
            ChestController chest = currentChest;
            ClosePuzzle();
            
            // Chama OnPuzzleSolved na referência guardada
            if (chest != null)
            {
                chest.OnPuzzleSolved();
            }
            else
            {
                Debug.LogError("ChestUIManager: Referência do baú foi perdida!");
            }
        }
        else
        {
            // Senha incorreta
            
            // Som de erro (opcional)
            if (AudioManager.Instance != null)
            {
                // AudioManager.Instance.PlaySFX("wrong_password");
            }
            
            ShowFeedback(wrongPasswordMessage, Color.red);
            
            // Limpa a entrada baseado no modo
            if (inputMode == PasswordInputMode.TextField && passwordInputField != null)
            {
                passwordInputField.text = "";
                passwordInputField.Select();
                passwordInputField.ActivateInputField();
            }
            else if (inputMode == PasswordInputMode.RotaryDials && passwordDials != null)
            {
                // Opcional: pode resetar os dials ou deixar como está
                // foreach (PasswordDial dial in passwordDials)
                // {
                //     if (dial != null) dial.Reset();
                // }
            }
        }
    }

    /// <summary>
    /// Mostra uma mensagem de feedback temporária.
    /// </summary>
    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
            feedbackText.gameObject.SetActive(true);
            feedbackTimer = feedbackDuration;
        }
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
