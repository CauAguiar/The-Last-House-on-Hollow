using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controller para o abajur interativo. O jogador pode usar a lâmpada (InventoryItem)
/// para colocá-la no abajur. Ao usar, o Light2D filho é ativado, o item de lâmpada
/// é consumido (removed do inventário), o jogador recebe uma recompensa (chave de brinquedo)
/// e o estado é persistido via GameStateManager.
/// </summary>
public class LampController : InteractableBase
{
    [Header("Requisito")]
    [Tooltip("Item de inventário que representa a lâmpada a ser colocada no abajur.")]
    [SerializeField] private InventoryItem lampItem;

    [Header("Recompensa")]
    [Tooltip("Item entregue ao jogador após colocar a lâmpada (ex: chave de brinquedo)")]
    [SerializeField] private InventoryItem rewardItem;
    [Tooltip("IDs de páginas do diário para coletar quando resolver (opcional)")]
    [SerializeField] private int[] diaryPageIds;

    [Header("Light2D")]
    [Tooltip("Light2D filho que será ativado quando a lâmpada for colocada.")]
    [SerializeField] private Light2D lampLight;
    [Tooltip("Intensidade alvo da Light2D ao ativar")]
    [SerializeField] private float lampLightTargetIntensity = 1f;

    [Header("Audio - Uso")]
    [SerializeField] private string useItemSfxName;
    [SerializeField] private float useItemSfxStart = 0f;
    [SerializeField] private float useItemSfxDuration = 0f;
    [SerializeField] private AudioManager.Category useItemSfxCategory = AudioManager.Category.SFX;
    [SerializeField] [Range(0f,1f)] private float useItemSfxVolume = 1f;

    [Tooltip("Delay (s) após usar antes de ativar a luz/entregar recompensa")]
    [SerializeField] private float postUseDelay = 0.15f;

    [Header("Persistência")]
    [SerializeField] private string uniqueId;

    private bool isFilled = false;

    private void Start()
    {
        if (string.IsNullOrEmpty(uniqueId)) uniqueId = System.Guid.NewGuid().ToString();
        isFilled = GameStateManager.Instance.IsCollected(uniqueId);

        if (isFilled)
        {
            ActivateLampLightImmediate();
        }
        else
        {
            // ensure light starts disabled
            if (lampLight != null)
            {
                try { lampLight.gameObject.SetActive(false); } catch { }
                lampLight.enabled = false;
            }
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid() { uniqueId = System.Guid.NewGuid().ToString(); }

    public override void OnUseItem(InventoryItem item)
    {
        if (item == null) { base.OnUseItem(item); return; }

        if (isFilled)
        {
            InteractionManager.Instance.ShowDialogue("A lâmpada já está colocada.");
            return;
        }

        if (item == lampItem)
        {
            // play use SFX
            if (!string.IsNullOrEmpty(useItemSfxName) && AudioManager.Instance != null)
            {
                if (useItemSfxDuration > 0f)
                    AudioManager.Instance.PlaySFXSlice(useItemSfxName, useItemSfxStart, useItemSfxDuration, useItemSfxVolume, useItemSfxCategory);
                else
                    AudioManager.Instance.PlaySFX(useItemSfxName, useItemSfxCategory, useItemSfxVolume);
            }

            // consume lamp item from inventory
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveItem(item);
            }

            // mark filled and persist
            isFilled = true;
            if (!string.IsNullOrEmpty(uniqueId)) GameStateManager.Instance.MarkAsCollected(uniqueId);

            // apply post-use
            if (postUseDelay > 0f) StartCoroutine(PostUseSequence()); else { ActivateLampLightImmediate(); GiveRewards(); }

            InteractionManager.Instance.ShowDialogue("Que haja luz! Opa, encontrei uma chave de brinquedo escondida nas sombras");
        }
        else
        {
            base.OnUseItem(item);
        }
    }

    private System.Collections.IEnumerator PostUseSequence()
    {
        yield return new WaitForSecondsRealtime(postUseDelay);
        ActivateLampLightImmediate();
        GiveRewards();
    }

    private void ActivateLampLightImmediate()
    {
        if (lampLight == null) return;
        try { lampLight.gameObject.SetActive(true); } catch { }
        lampLight.enabled = true;
        lampLight.intensity = lampLightTargetIntensity;
    }

    private void GiveRewards()
    {
        if (rewardItem != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(rewardItem);
        }

        if (diaryPageIds != null && diaryPageIds.Length > 0)
        {
            foreach (var id in diaryPageIds) JournalManager.Instance.CollectPage(id);
        }
    }

    public override void OnInspect()
    {
        if (isFilled)
        {
            InteractionManager.Instance.ShowDialogue("A lâmpada já está aqui, iluminando.");
            return;
        }
        base.OnInspect();
    }
}
