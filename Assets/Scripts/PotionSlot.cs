using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PotionSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configuração")]
    public Image potionImage; // A imagem visual da poção
    public GameObject selectionHighlight; // Uma borda/luz que aparece quando selecionado
    [Header("Highlight")]
    [Tooltip("Cor aplicada ao objeto de highlight quando selecionado. Se o objeto de highlight tiver um Image, a cor será aplicada nele.")]
    public Color selectedColor = new Color(1f, 0.9f, 0.4f, 1f);
    [Tooltip("Cor aplicada quando não selecionado. Por padrão tentamos deixar transparente/oculto.")]
    public Color deselectedColor = new Color(1f,1f,1f,0f);

    // O estado atual deste slot
    [HideInInspector] public PotionColor currentColor;
    private int slotIndex;
    private Image highlightImage;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    [Header("Hover")]
    [Tooltip("Fator de escala aplicado quando o mouse passa por cima (1 = sem mudança).")]
    public float hoverScale = 1.08f;
    [Tooltip("Duração em segundos da transição de escala ao entrar/sair.")]
    public float hoverScaleDuration = 0.12f;

    public void Setup(int index, PotionColor color, Sprite sprite)
    {
        slotIndex = index;
        currentColor = color;
        potionImage.sprite = sprite;
        SetSelected(false);
        originalScale = transform.localScale;
    }

    // Chamado pelo Manager para mudar o conteúdo visual deste slot
    public void UpdateContent(PotionColor newColor, Sprite newSprite)
    {
        currentColor = newColor;
        potionImage.sprite = newSprite;
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
        {
            // Se o objeto de highlight possui uma Image, aplicamos cor; caso contrário ativamos/desativamos o objeto
            if (highlightImage == null) highlightImage = selectionHighlight.GetComponent<Image>();
            if (highlightImage != null)
            {
                highlightImage.color = isSelected ? selectedColor : deselectedColor;
                // Mantemos o objeto ativo para que a imagem seja visível; caso deselectedColor seja transparente, ficará invisível.
                selectionHighlight.SetActive(true);
            }
            else
            {
                selectionHighlight.SetActive(isSelected);
            }
        }
        else
        {
            // Fallback: muda o tint da própria poção para indicar seleção (não ideal, mas evita nenhum feedback)
            if (potionImage != null)
                potionImage.color = isSelected ? selectedColor : Color.white;
        }
    }

    // Detecta o clique do mouse
    public void OnPointerClick(PointerEventData eventData)
    {
        if (PotionsUIManager.Instance == null)
        {
            Debug.LogWarning("PotionsUIManager.Instance is null when clicking a potion slot.");
            return;
        }
        Debug.Log($"PotionSlot clicked: index={slotIndex}, color={currentColor}");
        PotionsUIManager.Instance.OnSlotClicked(slotIndex);
    }

    // Hover handlers
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Smoothly scale up
        StartScaleCoroutine(originalScale * hoverScale, hoverScaleDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Smoothly scale back to original
        StartScaleCoroutine(originalScale, hoverScaleDuration);
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