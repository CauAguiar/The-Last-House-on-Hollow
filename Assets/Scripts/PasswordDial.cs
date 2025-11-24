using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Representa um mostrador individual do puzzle de senha com mostradores rotativos.
/// Cada dial pode ser girado para selecionar um número de 0 a 9.
/// </summary>
public class PasswordDial : MonoBehaviour
{
    [Header("Componentes UI")]
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;

    [Header("Configuração")]
    private int currentValue = 0;
    [SerializeField] private int minValue = 0;
    [SerializeField] private int maxValue = 9;

    [Header("Animação (Opcional)")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private RectTransform dialWheel; // Opcional: roda visual que gira

    private float targetRotation = 0f;

    private void Start()
    {
        if (upButton != null)
        {
            upButton.onClick.AddListener(IncrementValue);
        }

        if (downButton != null)
        {
            downButton.onClick.AddListener(DecrementValue);
        }

        UpdateDisplay();
    }

    private void Update()
    {
        // Animação suave da roda (opcional)
        if (dialWheel != null)
        {
            float currentRotation = dialWheel.localEulerAngles.z;
            float newRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * rotationSpeed);
            dialWheel.localEulerAngles = new Vector3(0, 0, newRotation);
        }
    }

    /// <summary>
    /// Incrementa o valor do dial (0-9, circular).
    /// </summary>
    public void IncrementValue()
    {
        currentValue++;
        if (currentValue > maxValue)
        {
            currentValue = minValue;
        }
        UpdateDisplay();
        UpdateRotation();

        // Som de clique (opcional)
        // Notify UI manager and play configured UI SFX (if present)
        if (ChestUIManager.Instance != null)
        {
            ChestUIManager.Instance.OnDialValueChanged();
        }
    }

    /// <summary>
    /// Decrementa o valor do dial (0-9, circular).
    /// </summary>
    public void DecrementValue()
    {
        currentValue--;
        if (currentValue < minValue)
        {
            currentValue = maxValue;
        }
        UpdateDisplay();
        UpdateRotation();

        // Som de clique (opcional)
        // Notify UI manager and play configured UI SFX (if present)
        if (ChestUIManager.Instance != null)
        {
            ChestUIManager.Instance.OnDialValueChanged();
        }
    }

    /// <summary>
    /// Define o valor do dial diretamente.
    /// </summary>
    public void SetValue(int value)
    {
        currentValue = Mathf.Clamp(value, minValue, maxValue);
        UpdateDisplay();
        UpdateRotation();
    }

    /// <summary>
    /// Retorna o valor atual do dial.
    /// </summary>
    public int GetValue()
    {
        return currentValue;
    }

    /// <summary>
    /// Reseta o dial para 0.
    /// </summary>
    public void Reset()
    {
        SetValue(0);
    }

    /// <summary>
    /// Atualiza o texto exibido.
    /// </summary>
    private void UpdateDisplay()
    {
        if (numberText != null)
        {
            numberText.text = currentValue.ToString();
        }
    }

    /// <summary>
    /// Atualiza a rotação visual da roda (opcional).
    /// Cada número = 36 graus de rotação (360/10).
    /// </summary>
    private void UpdateRotation()
    {
        if (dialWheel != null)
        {
            targetRotation = -currentValue * 36f; // Negativo para girar no sentido horário
        }
    }
}
