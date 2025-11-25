using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Slot simples para o puzzle de quadros (números). Mostra um número (TMP) e opcionalmente uma imagem.
/// Notifica o `FramesUIManager` quando clicado.
/// </summary>
public class FrameSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referências")]
    public Image frameImage;
    public TextMeshProUGUI numberLabel;

    [Header("Highlight")]
    public GameObject selectionHighlight;

    [HideInInspector] public int currentNumber = 0;
    private int slotIndex;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    [Header("Interaction")]
    [Tooltip("When false, this slot ignores pointer events.")]
    public bool interactable = true;

    [Header("Audio")]
    public string clickSfxName;
    public float clickSfxStart = 0f;
    public float clickSfxDuration = 0.12f;
    public AudioManager.Category clickSfxCategory = AudioManager.Category.UI;
    [Range(0f,1f)] public float clickSfxVolume = 1f;

    [Header("Hover")]
    public float hoverScale = 1.06f;
    public float hoverScaleDuration = 0.12f;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void Setup(int index, int number, Sprite sprite)
    {
        slotIndex = index;
        currentNumber = number;
        if (numberLabel != null)
        {
            numberLabel.gameObject.SetActive(true);
            numberLabel.text = number.ToString();
        }
        if (frameImage != null && sprite != null)
        {
            frameImage.sprite = sprite;
            frameImage.gameObject.SetActive(true);
        }
        SetSelected(false);
    }

    public void UpdateContent(int number, Sprite sprite)
    {
        currentNumber = number;
        if (numberLabel != null)
        {
            numberLabel.text = number.ToString();
        }
        if (frameImage != null && sprite != null)
        {
            frameImage.sprite = sprite;
            frameImage.gameObject.SetActive(true);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null) selectionHighlight.SetActive(isSelected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable) return;
        // Play click SFX slice if configured
        if (!string.IsNullOrEmpty(clickSfxName) && AudioManager.Instance != null)
        {
            if (clickSfxDuration > 0f)
                AudioManager.Instance.PlaySFXSlice(clickSfxName, clickSfxStart, clickSfxDuration, clickSfxVolume, clickSfxCategory);
            else
                AudioManager.Instance.PlaySFX(clickSfxName, clickSfxCategory, clickSfxVolume);
        }

        FramesUIManager.Instance?.OnSlotClicked(slotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable) return;
        StartScaleCoroutine(originalScale * hoverScale, hoverScaleDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!interactable) return;
        StartScaleCoroutine(originalScale, hoverScaleDuration);
    }

    /// <summary>
    /// Enable or disable interaction for this slot.
    /// </summary>
    public void SetInteractable(bool enabled)
    {
        interactable = enabled;
        // hide selection highlight if disabled
        if (!interactable && selectionHighlight != null) selectionHighlight.SetActive(false);
    }

    private void StartScaleCoroutine(Vector3 target, float duration)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(target, duration));
    }

    private System.Collections.IEnumerator ScaleRoutine(Vector3 target, float duration)
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        if (duration <= 0f)
        {
            transform.localScale = target;
            yield break;
        }
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        transform.localScale = target;
        scaleCoroutine = null;
    }
}
