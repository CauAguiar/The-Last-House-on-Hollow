using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controla um ponteiro do relógio na UI. Permite clicar e arrastar com "snapping".
/// </summary>
public class ClockHand : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public enum HandType { Hour, Minute }

    [Header("Tipo do Ponteiro")]
    [Tooltip("Define se este ponteiro representa horas ou minutos (em passos de 5 minutos).")]
    [SerializeField] private HandType handType = HandType.Minute;

    [HideInInspector] public int currentValue = 12; // 1..12 (horas ou índice de 5 minutos)

    private RectTransform rectTransform;
    private const float degreesPerStep = 30f; // 360 / 12
    private float angleOffset = 0f; // Caso precise ajustar visualmente

    [Header("Suavização Visual")]
    [SerializeField] private float smoothSpeed = 20f; // Velocidade do Lerp para a rotação visual

    private float targetAngle; // alvo em graus (0..360) no sentido horário
    private ClockUIManager clockUIManager; // Referência para o gerente (resolvida sob demanda)

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // Não confie totalmente na ordem de Awake; resolva sob demanda também
        clockUIManager = GetComponentInParent<ClockUIManager>();
        if (clockUIManager == null)
            clockUIManager = ClockUIManager.Instance; 

        // Inicializa o alvo de acordo com o valor inicial (12 -> 0 graus)
        int step = (currentValue % 12);
        if (step == 0) step = 12; // 12 como posição
        targetAngle = ((step % 12) * degreesPerStep) % 360f; // 12 -> 0
        // Força rotação inicial coerente
        rectTransform.localEulerAngles = new Vector3(0, 0, -(targetAngle + angleOffset));
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        UpdateHandRotation(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateHandRotation(eventData);
    }

    private void Update()
    {
        // Suaviza rotação para o ângulo alvo usando tempo real (UI pode estar com o jogo pausado)
        var currentZ = rectTransform.localEulerAngles.z;
        var desiredZ = -(targetAngle + angleOffset);
        float newZ = Mathf.LerpAngle(currentZ, desiredZ, Time.unscaledDeltaTime * smoothSpeed);
        rectTransform.localEulerAngles = new Vector3(0, 0, newZ);
    }

    private void UpdateHandRotation(PointerEventData eventData)
    {
        // Converte posição do cursor para espaço local do pai (centro do relógio)
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out localPos);

        // Angulo 0 em "12 horas" aumentando no sentido horário
        float rawAngle = Mathf.Atan2(localPos.x, localPos.y) * Mathf.Rad2Deg; // Atan2(x,y) dá 0 em cima e sentido horário positivo
        if (rawAngle < 0) rawAngle += 360f;

    // Snap para múltiplos de 30 graus (12 posições)
    int step = Mathf.RoundToInt(rawAngle / degreesPerStep) % 12; // 0..11
    float snappedAngle = step * degreesPerStep; // 0..330

        // Valor lógico (1..12)
        int newValue = (step == 0) ? 12 : step;

    // Define alvo visual (rotação suavizada acontecerá no Update)
    targetAngle = snappedAngle;

        if (newValue != currentValue)
        {
            currentValue = newValue;

            // Resolve gerente sob demanda caso ainda não esteja setado (ordem de Awake pode variar)
            var mgr = clockUIManager != null ? clockUIManager : (clockUIManager = (GetComponentInParent<ClockUIManager>() ?? ClockUIManager.Instance));
            if (mgr != null)
            {
                mgr.CheckSolution();
            }

            // DEBUG: reporta somente o que faz sentido para este ponteiro
            if (handType == HandType.Hour)
            {
                Debug.Log($"{gameObject.name} (HORAS) -> Hora: {currentValue} | Ângulo: {snappedAngle}");
            }
            else // Minute
            {
                int displayedMinutes = (currentValue % 12) * 5; // 12 -> 60
                if (displayedMinutes == 0) displayedMinutes = 60;
                Debug.Log($"{gameObject.name} (MINUTOS) -> Minutos: {displayedMinutes} (índice {currentValue}) | Ângulo: {snappedAngle}");
            }
        }
    }
}