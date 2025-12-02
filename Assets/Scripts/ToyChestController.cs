using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Baú de brinquedo que abre com a chave de brinquedo.
/// Ao abrir: consome a chave, troca sprite, entrega um item e/ou página do diário,
/// toca SFX e dispara um jumpscare full-screen (Image) com flash e som estourado.
/// Estado é persistido via GameStateManager.
/// </summary>
public class ToyChestController : InteractableBase
{
    [Header("Requisito")]
    [SerializeField] private InventoryItem toyKeyItem;

    [Header("Recompensa")]
    [SerializeField] private InventoryItem rewardItem;
    [SerializeField] private int[] diaryPageIds;

    [Header("Visual / Sprite")]
    [SerializeField] private Sprite openSprite;

    [Header("Jumpscare")]
    [Tooltip("Image full-screen que será usada no jumpscare (previamente desativada)")]
    [SerializeField] private Image jumpScareImage;
    [Tooltip("Duração total do jumpscare em segundos")]
    [SerializeField] [Range(0.1f, 30f)] private float jumpScareDuration = 3.0f;
    [Tooltip("Intervalo de piscar (s)")]
    [SerializeField] [Range(0.02f, 2f)] private float jumpScareFlashInterval = 0.12f;
    [Tooltip("Habilita sacudir a câmera durante o jumpscare")]
    [SerializeField] private bool enableCameraShake = true;
    [Tooltip("Intensidade do shake da câmera em unidades de posição (ex: 0.2)")]
    [SerializeField] private float cameraShakeIntensity = 0.25f;

    [Header("Audio - Uso")]
    [SerializeField] private string useSfxName;
    [SerializeField] private float useSfxStart = 0f;
    [SerializeField] private float useSfxDuration = 0f;
    [SerializeField] private AudioManager.Category useSfxCategory = AudioManager.Category.SFX;
    [SerializeField] [Range(0f,1f)] private float useSfxVolume = 1f;

    [Header("Audio - Jumpscare")]
    [SerializeField] private string jumpscareSfxName;
    [SerializeField] private float jumpscareSfxStart = 0f;
    [SerializeField] private float jumpscareSfxDuration = 0f;
    [SerializeField] private AudioManager.Category jumpscareSfxCategory = AudioManager.Category.SFX;
    [SerializeField] [Range(0f,1f)] private float jumpscareSfxVolume = 1f;

    [Header("Persistência")]
    [SerializeField] private string uniqueId;

    private bool isOpened = false;

    private void Start()
    {
        if (string.IsNullOrEmpty(uniqueId)) uniqueId = System.Guid.NewGuid().ToString();
        isOpened = GameStateManager.Instance.IsCollected(uniqueId);
        if (isOpened)
        {
            ApplyOpenVisuals();
        }
        // ensure jumpscare image is off
        if (jumpScareImage != null) jumpScareImage.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        if (jumpScareDuration < 0f) jumpScareDuration = 0f;
        if (jumpScareFlashInterval < 0.01f) jumpScareFlashInterval = 0.01f;
        if (cameraShakeIntensity < 0f) cameraShakeIntensity = 0f;
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid() { uniqueId = System.Guid.NewGuid().ToString(); }

    public override void OnUseItem(InventoryItem item)
    {
        if (item == null) { base.OnUseItem(item); return; }

        if (isOpened)
        {
            InteractionManager.Instance.ShowDialogue("O baú de brinquedo já foi aberto.");
            return;
        }

        if (item == toyKeyItem)
        {
            // play use SFX
            if (!string.IsNullOrEmpty(useSfxName) && AudioManager.Instance != null)
            {
                if (useSfxDuration > 0f)
                    AudioManager.Instance.PlaySFXSlice(useSfxName, useSfxStart, useSfxDuration, useSfxVolume, useSfxCategory);
                else
                    AudioManager.Instance.PlaySFX(useSfxName, useSfxCategory, useSfxVolume);
            }

            // consume the key
            if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveItem(item);

            // mark opened
            isOpened = true;
            if (!string.IsNullOrEmpty(uniqueId)) GameStateManager.Instance.MarkAsCollected(uniqueId);

            // apply visuals and give rewards
            ApplyOpenVisuals();
            GiveRewards();

            // trigger jumpscare (block input while active)
            StartCoroutine(PlayJumpscareRoutine());

            InteractionManager.Instance.ShowDialogue("FILHA DA PUTAAAA, DE NOVO. Tudo isso por uma boneca e um pedaço de diário.");
        }
        else
        {
            base.OnUseItem(item);
        }
    }

    private void ApplyOpenVisuals()
    {
        if (openSprite == null) return;
        if (spriteRenderer != null) spriteRenderer.sprite = openSprite;
        else
        {
            var sr = GetComponent<SpriteRenderer>(); if (sr != null) sr.sprite = openSprite;
        }
    }

    private void GiveRewards()
    {
        if (rewardItem != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(rewardItem);
        }
        if (diaryPageIds != null && diaryPageIds.Length > 0)
        {
            foreach (int id in diaryPageIds) JournalManager.Instance.CollectPage(id);
        }
    }

    private System.Collections.IEnumerator PlayJumpscareRoutine()
    {
        // block input
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.LockMovement();
        UIInputBlocker.Block("ToyChestJumpscare");
        GamePauseManager.Pause("ToyChestJumpscare");

        // play jumpscare SFX (loud)
        if (!string.IsNullOrEmpty(jumpscareSfxName) && AudioManager.Instance != null)
        {
            if (jumpscareSfxDuration > 0f)
                AudioManager.Instance.PlaySFXSlice(jumpscareSfxName, jumpscareSfxStart, jumpscareSfxDuration, jumpscareSfxVolume, jumpscareSfxCategory);
            else
                AudioManager.Instance.PlaySFX(jumpscareSfxName, jumpscareSfxCategory, jumpscareSfxVolume);
        }

        // show and flash image
        if (jumpScareImage != null)
        {
            jumpScareImage.gameObject.SetActive(true);
            Color baseColor = jumpScareImage.color;
            float elapsed = 0f;
            bool visible = true;

            // camera original pos
            Camera mainCam = Camera.main;
            Vector3 camOriginalPos = mainCam != null ? mainCam.transform.localPosition : Vector3.zero;

            while (elapsed < jumpScareDuration)
            {
                // toggle visibility on each interval
                visible = !visible;
                float alpha = visible ? 1f : 0f;
                jumpScareImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

                // perform camera shake for the duration of the interval
                float t = 0f;
                while (t < jumpScareFlashInterval && elapsed < jumpScareDuration)
                {
                    float dt = Time.unscaledDeltaTime;
                    t += dt;
                    elapsed += dt;

                    if (enableCameraShake && mainCam != null)
                    {
                        Vector2 rnd = Random.insideUnitCircle * cameraShakeIntensity;
                        mainCam.transform.localPosition = camOriginalPos + new Vector3(rnd.x, rnd.y, 0f);
                    }

                    yield return null;
                }

                // restore camera position at end of interval to avoid drift
                if (enableCameraShake && mainCam != null)
                {
                    mainCam.transform.localPosition = camOriginalPos;
                }
            }

            // ensure hidden after
            jumpScareImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            jumpScareImage.gameObject.SetActive(false);
        }
        else
        {
            // if no image assigned, still wait the duration so reward timing is consistent
            yield return new WaitForSecondsRealtime(jumpScareDuration);
        }

        // unblock input
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.UnlockMovement();
        UIInputBlocker.Unblock("ToyChestJumpscare");
        GamePauseManager.Unpause("ToyChestJumpscare");
    }

    public override void OnInspect()
    {
        if (isOpened)
        {
            InteractionManager.Instance.ShowDialogue("O baú de brinquedo está vazio.");
            return;
        }
        base.OnInspect();
    }
}
