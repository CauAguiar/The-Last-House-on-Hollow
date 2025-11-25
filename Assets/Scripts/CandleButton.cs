using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Componente para cada botão/vela na UI do candelabro.
/// Controla visual (sprite ou Animator) e notifica o manager quando clicado.
/// </summary>
public class CandleButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    public Image candleImage; // imagem padrão (apagada)
    public Sprite unlitSprite;
    [Header("GIF/Frames")]
    [Tooltip("Sprites que compõem a animação da vela acesa (frames do 'gif').")]
    public Sprite[] litFrames;
    [Tooltip("Frames por segundo da animação da vela acesa. Se <=0, usa 12 fps.")]
    public float litFrameRate = 12f;

    [Header("Index")]
    public int index = 0; // 0-based index

    [Header("Resolution / Display")]
    [Tooltip("If true, the Image will call SetNativeSize() when switching to lit frames to preserve sprite resolution.")]
    public bool useNativeSizeOnFrame = true;
    [Tooltip("Scale multiplier applied after SetNativeSize() so you can fine-tune visual size.")]
    public float nativeScale = 1f;
    [Tooltip("Keep Image.preserveAspect = true for correct aspect ratio when using native size.")]
    public bool preserveAspect = true;

    [Header("Audio")]
    public string clickSfxName;
    public float clickSfxStart = 0f;
    public float clickSfxDuration = 0.12f;
    public AudioManager.Category clickSfxCategory = AudioManager.Category.UI;
    [Range(0f,1f)] public float clickSfxVolume = 1f;

    private bool isLit = false;
    private Coroutine playCoroutine;
    private Vector3 originalScale = Vector3.one;

    private void Start()
    {
        // ensure initial visuals
        if (candleImage != null && unlitSprite != null)
        {
            candleImage.sprite = unlitSprite;
            candleImage.gameObject.SetActive(true);
        }
        // preserve aspect if requested
        if (candleImage != null)
        {
            candleImage.preserveAspect = preserveAspect;
            originalScale = candleImage.transform.localScale;
        }
        // no animator by default; frames used when lit
    }

    public void SetLit(bool lit)
    {
        isLit = lit;
        if (lit)
        {
            // start frame animation if frames are provided
            if (litFrames != null && litFrames.Length > 0 && candleImage != null)
            {
                if (playCoroutine != null) StopCoroutine(playCoroutine);
                playCoroutine = StartCoroutine(PlayLitFrames());
            }
        }
        else
        {
            // stop animation and restore unlit sprite
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }
            if (candleImage != null && unlitSprite != null)
            {
                candleImage.sprite = unlitSprite;
                // restore original scale if we changed it for native size
                candleImage.transform.localScale = originalScale;
            }
        }
    }

    public bool IsLit() => isLit;

    public void OnPointerClick(PointerEventData eventData)
    {
        // Only play click SFX if the chandelier UI/controller allows lighting (avoids sound when player lacks required item)
        bool playSfx = true;
        if (ChandelierUIManager.Instance != null)
        {
            playSfx = ChandelierUIManager.Instance.CanLight(index);
        }

        if (playSfx && !string.IsNullOrEmpty(clickSfxName) && AudioManager.Instance != null)
        {
            if (clickSfxDuration > 0f)
                AudioManager.Instance.PlaySFXSlice(clickSfxName, clickSfxStart, clickSfxDuration, clickSfxVolume, clickSfxCategory);
            else
                AudioManager.Instance.PlaySFX(clickSfxName, clickSfxCategory, clickSfxVolume);
        }

        ChandelierUIManager.Instance?.OnCandleClicked(index);
    }

    private System.Collections.IEnumerator PlayLitFrames()
    {
        int len = litFrames.Length;
        if (len == 0) yield break;
        float fps = litFrameRate > 0f ? litFrameRate : 12f;
        float frameTime = 1f / fps;
        int idx = 0;
        bool appliedNativeSize = false;
        while (isLit)
        {
            if (candleImage != null && litFrames[idx] != null)
            {
                candleImage.sprite = litFrames[idx];
                // apply native size once when switching to lit frames to preserve higher-resolution sprites
                if (useNativeSizeOnFrame && !appliedNativeSize)
                {
                    appliedNativeSize = true;
                    candleImage.SetNativeSize();
                    // apply user scale multiplier
                    candleImage.transform.localScale = originalScale * nativeScale;
                }
            }
            idx = (idx + 1) % len;
            float t = 0f;
            while (t < frameTime)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
