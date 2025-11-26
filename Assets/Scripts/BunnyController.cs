using UnityEngine;

/// <summary>
/// Controller para o coelho de pelúcia interativo.
/// Uso: jogador usa a faca (InventoryItem) sem consumi-la; o coelho troca de sprite
/// e entrega a `rewardItem` (ex: Lâmpada). Estado é salvo via GameStateManager.
/// </summary>
public class BunnyController : InteractableBase
{
    [Header("Requisito")]
    [Tooltip("Item necessário para cortar o coelho (ex: Faca)")]
    [SerializeField] private InventoryItem knifeItem;

    [Header("Recompensa")]
    [Tooltip("Item a ser dado ao jogador ao cortar o coelho (ex: Lâmpada)")]
    [SerializeField] private InventoryItem rewardItem;
    [Tooltip("IDs de páginas do diário para coletar quando resolver (opcional)")]
    [SerializeField] private int[] diaryPageIds;

    [Header("Visual")]
    [Tooltip("Sprite final depois do uso (coelho cortado)")]
    [SerializeField] private Sprite usedSprite;
    [Tooltip("SpriteRenderer alvo; se for UI, atribuir Image em vez disso no futuro")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [Tooltip("Se o visual for UI, pode ser um Image (opcional)")]
    [SerializeField] private UnityEngine.UI.Image targetImage;

    [Header("Audio - Uso")]
    [SerializeField] private string useItemSfxName;
    [SerializeField] private float useItemSfxStart = 0f;
    [SerializeField] private float useItemSfxDuration = 0f;
    [SerializeField] private AudioManager.Category useItemSfxCategory = AudioManager.Category.UI;
    [SerializeField] [Range(0f,1f)] private float useItemSfxVolume = 1f;
    [Tooltip("Delay (s) após usar antes de aplicar sprite/entregar item")]
    [SerializeField] private float postUseDelay = 0.2f;

    [Header("Persistência")]
    [SerializeField] private string uniqueId;

    private bool isUsed = false;

    private void Start()
    {
        if (string.IsNullOrEmpty(uniqueId)) uniqueId = System.Guid.NewGuid().ToString();

        isUsed = GameStateManager.Instance.IsCollected(uniqueId);
        if (isUsed)
        {
            ApplyUsedVisuals();
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid() { uniqueId = System.Guid.NewGuid().ToString(); }

    public override void OnUseItem(InventoryItem item)
    {
        if (item == null) { base.OnUseItem(item); return; }

        if (isUsed)
        {
            InteractionManager.Instance.ShowDialogue("Não há mais nada aqui.");
            return;
        }

        if (item == knifeItem)
        {
            // Play use SFX (slice support)
            if (!string.IsNullOrEmpty(useItemSfxName) && AudioManager.Instance != null)
            {
                if (useItemSfxDuration > 0f)
                    AudioManager.Instance.PlaySFXSlice(useItemSfxName, useItemSfxStart, useItemSfxDuration, useItemSfxVolume, useItemSfxCategory);
                else
                    AudioManager.Instance.PlaySFX(useItemSfxName, useItemSfxCategory, useItemSfxVolume);
            }

            // Knife should remain in inventory (do not remove)

            // Mark used and persist
            isUsed = true;
            if (!string.IsNullOrEmpty(uniqueId)) GameStateManager.Instance.MarkAsCollected(uniqueId);

            // Play post-use sequence (swap sprite, give item)
            if (postUseDelay > 0f)
            {
                StartCoroutine(PostUseSequence());
            }
            else
            {
                ApplyUsedVisuals();
                GiveRewards();
            }

            InteractionManager.Instance.ShowDialogue("Acho que vou vomitar. Mas achei uma Lâmpada");
        }
        else
        {
            base.OnUseItem(item);
        }
    }

    private System.Collections.IEnumerator PostUseSequence()
    {
        yield return new WaitForSecondsRealtime(postUseDelay);
        ApplyUsedVisuals();
        GiveRewards();
    }

    private void ApplyUsedVisuals()
    {
        if (usedSprite == null) return;
        if (targetSpriteRenderer != null) { targetSpriteRenderer.sprite = usedSprite; return; }
        if (targetImage != null) { targetImage.sprite = usedSprite; return; }
        var sr = GetComponent<SpriteRenderer>(); if (sr != null) sr.sprite = usedSprite;
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

    public override void OnInspect()
    {
        if (isUsed)
        {
            InteractionManager.Instance.ShowDialogue("Não há mais nada aqui.");
            return;
        }
        base.OnInspect();
    }
}
