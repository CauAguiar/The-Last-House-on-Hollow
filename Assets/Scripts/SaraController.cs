using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller para o objeto interativo "Sara".
/// Funciona similar ao espelho: ao usar o item `Toalha` o jogador dispara
/// uma animação de susto (apenas uma vez), o sprite/frame do objeto é trocado
/// e um TMP (dica) é revelado com um pequeno efeito. Estado é persistido via GameStateManager.
/// </summary>
public class SaraController : InteractableBase
{
    [Header("Item / Requisito")]
    [Tooltip("Item do inventário (ScriptableObject) que deve ser usado para ativar Sara (Toalha).")]
    [SerializeField] private InventoryItem towelItem;
    [Tooltip("Se true, o item será removido do inventário ao usar.")]
    [SerializeField] private bool consumeItemOnUse = true;

    [Header("Uso - SFX")]
    [Tooltip("SFX tocado ao usar o item no objeto (opcional)")]
    [SerializeField] private string useItemSfxName;
    [SerializeField] private float useItemSfxStart = 0f;
    [SerializeField] private float useItemSfxDuration = 0f;
    [SerializeField] private AudioManager.Category useItemSfxCategory = AudioManager.Category.UI;
    [Range(0f,1f)] [SerializeField] private float useItemSfxVolume = 1f;
    [Tooltip("Delay (s) entre usar o item e aplicar sprite/revelar (útil para sincronizar animação)")]
    [SerializeField] private float postUseDelay = 0.15f;

    [Header("Visual / Sprite")]
    [Tooltip("Sprite a ser aplicado ao objeto quando Sara for ativada (susto).")]
    [SerializeField] private Sprite scaredSprite;
    [Tooltip("Opcional: SpriteRenderer alvo para trocar o sprite diretamente (se não fornecido tentará usar este GameObject).")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [Tooltip("Opcional: Se o objeto visual for UI, atribua aqui o Image alvo em vez do SpriteRenderer.")]
    [SerializeField] private Image targetImage;

    // Animator removido: usamos animação por frames (GIF-like) em seu lugar

    [Header("GIF/Frames (Susto)")]
    [Tooltip("Sprites que compõem a animação de susto. Se preenchido, serão usados em vez do Animator.")]
    [SerializeField] private Sprite[] scaredFrames;
    [Tooltip("Frames por segundo para a animação de susto. Se <=0, usa 12 fps.")]
    [SerializeField] private float scaredFrameRate = 12f;
    [Tooltip("Se true, chama SetNativeSize() na Image alvo ao aplicar os frames e aplica `nativeScale` multiplicador.")]
    [SerializeField] private bool useNativeSizeOnFrame = true;
    [Tooltip("Multiplicador de escala aplicado após SetNativeSize().")]
    [SerializeField] private float nativeScale = 1f;
    [Tooltip("Preserva aspect ratio ao usar Image.SetNativeSize().")]
    [SerializeField] private bool preserveAspect = true;

    [Header("Dica (TextMeshPro)")]
    [Tooltip("GameObject que contém o TMP com a dica. Começa oculto e será revelado.")]
    [SerializeField] private GameObject clueTextObject;
    [Tooltip("CanvasGroup opcional para fazer fade da dica.")]
    [SerializeField] private CanvasGroup clueCanvasGroup;
    [Tooltip("Duração do efeito de revelação (s)")]
    [SerializeField] private float revealDuration = 0.6f;
    [Tooltip("Escala inicial (oculta) da dica antes do pop/fade.")]
    [SerializeField] private float hiddenScale = 0.6f;
    [Tooltip("Nome do som no SoundBank para tocar ao revelar a dica (opcional)")]
    [SerializeField] private string revealSoundName;
    [SerializeField] private AudioManager.Category revealSoundCategory = AudioManager.Category.UI;
    [Header("Reveal Macabre (TMP)")]
    [Tooltip("Se true, aplica efeito macabro ao texto TMP (typewriter + flicker)")]
    [SerializeField] private bool useMacabreTextReveal = true;
    [Tooltip("Delay entre cada caractere ao revelar (s)")]
    [SerializeField] private float macabreCharDelay = 0.03f;
    [Tooltip("Força do jitter alpha durante reveal (0..1)")]
    [SerializeField] [Range(0f,0.8f)] private float macabreJitter = 0.15f;
    [Tooltip("Quantos flickers curtos durante o reveal")]
    [SerializeField] private int macabreFlickerCount = 3;

    [Header("Estado / Persistência")]
    [Tooltip("Unique ID usado pelo GameStateManager para marcar que Sara já foi ativada.")]
    [SerializeField] private string uniqueId;

    private bool isActivated = false;
    private bool animationPlayed = false; // evita tocar animação novamente
    private Coroutine playFrameCoroutine;
    private Vector3 originalVisualScale = Vector3.one;
    private bool revealAfterAnimation = false;

    private void Start()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
        }

        isActivated = GameStateManager.Instance.IsCollected(uniqueId);
        if (isActivated)
        {
            // Apply final visuals and ensure clue visible (do not trigger reveal sequence)
            animationPlayed = true; // avoid replaying animation when loading saved state
            ApplyScaredVisuals(false);
            if (clueTextObject != null) clueTextObject.SetActive(true);
            if (clueCanvasGroup != null) clueCanvasGroup.alpha = 1f;
        }
        else
        {
            if (clueTextObject != null) clueTextObject.SetActive(false);
            if (clueCanvasGroup != null) clueCanvasGroup.alpha = 0f;
        }

        // capture original visual scale for SetNativeSize fallbacks
        if (targetImage != null)
        {
            originalVisualScale = targetImage.transform.localScale;
            targetImage.preserveAspect = preserveAspect;
        }
        else if (targetSpriteRenderer != null)
        {
            originalVisualScale = targetSpriteRenderer.transform.localScale;
        }
        else
        {
            var srFallback = GetComponent<SpriteRenderer>();
            if (srFallback != null) originalVisualScale = srFallback.transform.localScale;
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    public override void OnUseItem(InventoryItem item)
    {
        if (item == null)
        {
            base.OnUseItem(item);
            return;
        }

        if (isActivated)
        {
            InteractionManager.Instance.ShowDialogue("Só sobrou BETA");
            return;
        }

        if (item == towelItem)
        {
            // play use-item SFX (slice support)
            if (!string.IsNullOrEmpty(useItemSfxName) && AudioManager.Instance != null)
            {
                if (useItemSfxDuration > 0f)
                    AudioManager.Instance.PlaySFXSlice(useItemSfxName, useItemSfxStart, useItemSfxDuration, useItemSfxVolume, useItemSfxCategory);
                else
                    AudioManager.Instance.PlaySFX(useItemSfxName, useItemSfxCategory, useItemSfxVolume);
            }

            // optionally consume the item from inventory
            if (consumeItemOnUse && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveItem(item);
            }

            // Activate Sara's reaction once
            isActivated = true;
            GameStateManager.Instance.MarkAsCollected(uniqueId);

            // optionally wait a bit to synchronize with animation
            if (postUseDelay > 0f)
            {
                StartCoroutine(PostUseSequence());
            }
            else
            {
                // immediate apply visuals and trigger reveal after visuals finish
                ApplyScaredVisuals(true);
            }

            InteractionManager.Instance.ShowDialogue("FILHA DA PUTA! Até perdi a toalha");
        }
        else
        {
            base.OnUseItem(item);
        }
    }

    private System.Collections.IEnumerator PostUseSequence()
    {
        yield return new WaitForSecondsRealtime(postUseDelay);
        ApplyScaredVisuals(true);
    }

    private void PrepareAndRevealClue()
    {
        // reveal the clue (with effect)
        PrepareRevealVisuals();
        if (clueTextObject != null) clueTextObject.SetActive(true);
        if (useMacabreTextReveal)
        {
            StartCoroutine(RevealTMPMacabre());
        }
        else
        {
            StartCoroutine(RevealRoutine());
        }
    }

    private void ApplyScaredVisuals(bool revealAfter)
    {
        // If already played animation, just ensure final sprite applied
        if (animationPlayed)
        {
            if (scaredSprite != null)
            {
                if (targetSpriteRenderer != null) { targetSpriteRenderer.sprite = scaredSprite; }
                else if (targetImage != null) { targetImage.sprite = scaredSprite; }
                else { var sr = GetComponent<SpriteRenderer>(); if (sr != null) sr.sprite = scaredSprite; }
            }
            // If caller requested a reveal but animation already played, reveal immediately
            if (revealAfter) PrepareAndRevealClue();
            return;
        }

        // If frames are provided and we haven't played them yet, play them once and optionally reveal after
        if (scaredFrames != null && scaredFrames.Length > 0)
        {
            revealAfterAnimation = revealAfter;
            if (playFrameCoroutine != null) StopCoroutine(playFrameCoroutine);
            playFrameCoroutine = StartCoroutine(PlayScaredFramesOnce());
            return;
        }

        // No frames: apply final sprite immediately and optionally reveal
        animationPlayed = true;
        if (scaredSprite != null)
        {
            if (targetSpriteRenderer != null) { targetSpriteRenderer.sprite = scaredSprite; }
            else if (targetImage != null) { targetImage.sprite = scaredSprite; }
            else { var sr = GetComponent<SpriteRenderer>(); if (sr != null) sr.sprite = scaredSprite; }
        }
        if (revealAfter) PrepareAndRevealClue();
    }

    private void PrepareRevealVisuals()
    {
        if (clueTextObject == null) return;
        var tr = clueTextObject.transform as RectTransform;
        if (tr != null)
        {
            tr.localScale = Vector3.one * hiddenScale;
        }
        if (clueCanvasGroup != null)
        {
            clueCanvasGroup.alpha = 0f;
        }
    }

    private System.Collections.IEnumerator RevealRoutine()
    {
        float t = 0f;
        var tr = clueTextObject.transform as RectTransform;
        // play reveal sound
        if (!string.IsNullOrEmpty(revealSoundName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(revealSoundName, revealSoundCategory, 1f);
        }
        while (t < revealDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / revealDuration);
            // Ease-out scale
            float scale = Mathf.Lerp(hiddenScale, 1f, 1f - Mathf.Pow(1f - normalized, 3f));
            if (tr != null) tr.localScale = Vector3.one * scale;
            if (clueCanvasGroup != null)
            {
                clueCanvasGroup.alpha = normalized;
            }
            yield return null;
        }
        if (tr != null) tr.localScale = Vector3.one;
        if (clueCanvasGroup != null) clueCanvasGroup.alpha = 1f;
    }

    private System.Collections.IEnumerator RevealTMPMacabre()
    {
        // Try to find TextMeshProUGUI inside clueTextObject
        var tmp = clueTextObject.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmp == null)
        {
            // fallback to normal reveal
            yield return RevealRoutine();
            yield break;
        }

        // play reveal sound
        if (!string.IsNullOrEmpty(revealSoundName) && AudioManager.Instance != null)
        {
            if (revealSoundDurationAvailable())
            {
                // if there is slicing info, try to use PlaySFXSlice if available (no separate fields used here)
                AudioManager.Instance.PlaySFX(revealSoundName, revealSoundCategory, 1f);
            }
            else
            {
                AudioManager.Instance.PlaySFX(revealSoundName, revealSoundCategory, 1f);
            }
        }

        // prepare visuals
        var tr = clueTextObject.transform as RectTransform;
        if (tr != null) tr.localScale = Vector3.one * hiddenScale;
        if (clueCanvasGroup != null) clueCanvasGroup.alpha = 0f;

        string full = tmp.text;
        tmp.maxVisibleCharacters = 0;
        int total = full.Length;
        int flickersDone = 0;
        int visible = 0;

        while (visible < total)
        {
            visible++;
            tmp.maxVisibleCharacters = visible;

            // small jitter on color/alpha
            if (macabreJitter > 0f)
            {
                Color c = tmp.color;
                float jitterAlpha = Mathf.Clamp01(1f - Random.Range(0f, macabreJitter));
                tmp.color = new Color(c.r * 0.9f, c.g * 0.9f, c.b * 0.9f, jitterAlpha);
            }

            // occasional flicker
            if (macabreFlickerCount > 0 && flickersDone < macabreFlickerCount && Random.value < 0.05f)
            {
                flickersDone++;
                float savedAlpha = tmp.color.a;
                tmp.color = new Color(tmp.color.r * 0.5f, tmp.color.g * 0.5f, tmp.color.b * 0.5f, 0.2f);
                yield return new WaitForSecondsRealtime(0.06f);
                tmp.color = new Color(tmp.color.r * 2f, tmp.color.g * 2f, tmp.color.b * 2f, savedAlpha);
            }

            // scale/pop effect along the way
            if (tr != null)
            {
                float pop = Mathf.Lerp(hiddenScale, 1f, (float)visible / total);
                tr.localScale = Vector3.one * pop;
            }

            if (clueCanvasGroup != null)
            {
                clueCanvasGroup.alpha = Mathf.Clamp01((float)visible / total);
            }

            yield return new WaitForSecondsRealtime(macabreCharDelay);
        }

        // finalize
        tmp.maxVisibleCharacters = total;
        if (tr != null) tr.localScale = Vector3.one;
        if (clueCanvasGroup != null) clueCanvasGroup.alpha = 1f;
        // ensure TMP color restored (no persistent jitter)
        tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f);
    }

    private System.Collections.IEnumerator PlayScaredFramesOnce()
    {
        if (scaredFrames == null || scaredFrames.Length == 0)
            yield break;

        animationPlayed = true; // mark immediately so re-entry won't restart
        float fps = scaredFrameRate > 0f ? scaredFrameRate : 12f;
        float frameTime = 1f / fps;
        bool appliedNative = false;

        for (int i = 0; i < scaredFrames.Length; i++)
        {
            var frame = scaredFrames[i];
            if (frame != null)
            {
                if (targetImage != null)
                {
                    targetImage.sprite = frame;
                    if (useNativeSizeOnFrame && !appliedNative)
                    {
                        appliedNative = true;
                        targetImage.SetNativeSize();
                        targetImage.transform.localScale = originalVisualScale * nativeScale;
                    }
                }
                else if (targetSpriteRenderer != null)
                {
                    targetSpriteRenderer.sprite = frame;
                    if (useNativeSizeOnFrame && !appliedNative)
                    {
                        appliedNative = true;
                        targetSpriteRenderer.transform.localScale = originalVisualScale * nativeScale;
                    }
                }
                else
                {
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = frame;
                        if (useNativeSizeOnFrame && !appliedNative)
                        {
                            appliedNative = true;
                            sr.transform.localScale = originalVisualScale * nativeScale;
                        }
                    }
                }
            }

            float t = 0f;
            while (t < frameTime)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // after playing frames once, ensure final sprite is the scaredSprite (if present)
        if (scaredSprite != null)
        {
            if (targetImage != null) targetImage.sprite = scaredSprite;
            else if (targetSpriteRenderer != null) targetSpriteRenderer.sprite = scaredSprite;
            else
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = scaredSprite;
            }
        }

        playFrameCoroutine = null;

        // if requested, reveal clue after animation
        if (revealAfterAnimation)
        {
            revealAfterAnimation = false;
            PrepareAndRevealClue();
        }
    }

    private bool revealSoundDurationAvailable()
    {
        // placeholder for future: we don't have separate slice info here, return false
        return false;
    }

    public override void OnInspect()
    {
        if (isActivated && clueTextObject != null)
        {
            InteractionManager.Instance.ShowDialogue("Só sobrou BETA");
            return;
        }
        base.OnInspect();
    }
}
