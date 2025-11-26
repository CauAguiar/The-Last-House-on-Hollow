using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dial rotativo que exibe letras (usado pelo OfficeChestUIManager).
/// Funciona de forma similar a PasswordDial, mas trabalha com um conjunto de caracteres.
/// </summary>
public class OfficePasswordDial : MonoBehaviour
{
    [Header("Componentes UI")]
    [SerializeField] private TextMeshProUGUI letterText;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;

    [Header("Configuração")]
    [Tooltip("Letras disponíveis para este dial (ordem).")]
    public string[] letters = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };

    private int currentIndex = 0;

    [Header("Animação/Opcional")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private RectTransform dialWheel; // opcional

    private float targetRotation = 0f;

    private void Start()
    {
        if (upButton != null) upButton.onClick.AddListener(IncrementValue);
        if (downButton != null) downButton.onClick.AddListener(DecrementValue);
        UpdateDisplay();
    }

    private void Update()
    {
        if (dialWheel != null)
        {
            float currentRotation = dialWheel.localEulerAngles.z;
            float newRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * rotationSpeed);
            dialWheel.localEulerAngles = new Vector3(0, 0, newRotation);
        }
    }

    public void IncrementValue()
    {
        currentIndex++;
        if (currentIndex >= letters.Length) currentIndex = 0;
        UpdateDisplay();
        UpdateRotation();
        if (OfficeChestUIManager.Instance != null) OfficeChestUIManager.Instance.OnDialValueChanged();
    }

    public void DecrementValue()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = letters.Length - 1;
        UpdateDisplay();
        UpdateRotation();
        if (OfficeChestUIManager.Instance != null) OfficeChestUIManager.Instance.OnDialValueChanged();
    }

    public void SetLetters(string[] newLetters)
    {
        if (newLetters == null || newLetters.Length == 0) return;
        letters = newLetters;
        currentIndex = Mathf.Clamp(currentIndex, 0, letters.Length - 1);
        UpdateDisplay();
    }

    public string GetChar()
    {
        if (letters == null || letters.Length == 0) return "";
        return letters[currentIndex];
    }

    public int GetIndex()
    {
        return currentIndex;
    }

    public void Reset()
    {
        currentIndex = 0;
        UpdateDisplay();
        UpdateRotation();
    }

    private void UpdateDisplay()
    {
        if (letterText != null)
        {
            if (letters != null && letters.Length > 0)
                letterText.text = letters[currentIndex];
            else
                letterText.text = "";
        }
    }

    private void UpdateRotation()
    {
        if (dialWheel != null)
        {
            if (letters != null && letters.Length > 0)
                targetRotation = -currentIndex * (360f / letters.Length);
            else
                targetRotation = 0f;
        }
    }
}
