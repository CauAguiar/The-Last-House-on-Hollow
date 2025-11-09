using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controla um ponteiro do relógio na UI. Permite clicar e arrastar com "snapping".
/// </summary>
public class ClockHand : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [HideInInspector] public int currentValue = 12;

    private RectTransform rectTransform;
    private float degreesPerStep = 30f;
    private float angleOffset = 0f;

    // ----- NOVA LINHA -----
    private ClockUIManager clockUIManager; // Referência para o gerente

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // ----- NOVA LINHA -----
        // Pega a referência do Singleton
        clockUIManager = ClockUIManager.Instance; 
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        UpdateHandRotation(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateHandRotation(eventData);
    }

    private void UpdateHandRotation(PointerEventData eventData)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent.GetComponent<RectTransform>(), 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPos);

        float angle = (Mathf.Atan2(localPos.x, localPos.y) * Mathf.Rad2Deg);
        angle *= -1; 

        float snappedAngle = Mathf.Round(angle / degreesPerStep) * degreesPerStep;
        rectTransform.localEulerAngles = new Vector3(0, 0, snappedAngle + angleOffset);

        int step = Mathf.RoundToInt(snappedAngle / degreesPerStep);
        
        int newValue = (step <= 0) ? (12 + step) : step;
        if (newValue == 0) newValue = 12;

        // ----- LÓGICA ATUALIZADA -----
        // Se o valor realmente mudou...
        if (newValue != currentValue)
        {
            currentValue = newValue;
            
            // ...avisa o gerente para checar a solução.
            if (clockUIManager != null)
            {
                clockUIManager.CheckSolution();
            }
        }
    }
}