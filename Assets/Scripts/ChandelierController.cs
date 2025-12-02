using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controller em cena para o puzzle do candelabro. Guarda estado e configurações (item requisito e recompensa).
/// </summary>
public class ChandelierController : InteractableBase
{
    [Header("Recompensas")]
    [Tooltip("Item dado ao completar o puzzle")]
    public InventoryItem rewardItem;

    [Header("Requisito")]
    [Tooltip("Item necessário no inventário para permitir acender velas (não consumido)")]
    public InventoryItem requiredLitCandle;

    [Header("Estado")]
    [SerializeField] private string uniqueId;
    private bool isSolved = false;

    private void Start()
    {
        isSolved = GameStateManager.Instance.IsCollected(uniqueId);
        if (isSolved)
        {
            ApplyLitSprite();
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    public override void OnInspect()
    {
        if (isSolved)
        {
            if (!string.IsNullOrEmpty(inspectionText))
            {
                InteractionManager.Instance.ShowDialogue(inspectionText);
            }
            else
            {
                InteractionManager.Instance.ShowDialogue("O candelabro já está aceso.");
            }
            return;
        }

        if (ChandelierUIManager.Instance != null)
        {
            ChandelierUIManager.Instance.OpenPuzzle(this);
        }
    }

    // Bypass context menu; interact should directly open the puzzle UI
    public override void Interact()
    {
        OnInspect();
    }

    public override bool CanShowContextMenu()
    {
        return false;
    }

    public void OnPuzzleSolved()
    {
        if (isSolved) return;
        isSolved = true;
        GameStateManager.Instance.MarkAsCollected(uniqueId);

        // Update chandelier visual to the lit sprite if configured
        ApplyLitSprite();

        if (rewardItem != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(rewardItem);
        }

        InteractionManager.Instance.ShowDialogue("Consegui acender, uma chave caiu de um compartimento");
    }

    [Header("Visual: Candelabro")]
    [Tooltip("Sprite que representa o candelabro aceso. Será aplicado ao SpriteRenderer or Image do objeto abaixo.")]
    public Sprite litChandelierSprite;
    [Tooltip("Objeto do candelabro na cena. Pode ser usado para localizar o SpriteRenderer ou Image.")]
    public GameObject chandelierObject;
    [Tooltip("Opcional: atribua diretamente um SpriteRenderer se preferir.")]
    public SpriteRenderer chandelierSpriteRenderer;
    [Tooltip("Opcional: 2D Light (Light2D) do tipo Spot que será ativada quando o candelabro for aceso.")]
    public Light2D chandelierSpotLight;
    [Tooltip("Intensidade alvo para a Light2D ao acender (aplicada imediatamente).")]
    public float chandelierLightTargetIntensity = 1f;

    private void ApplyLitSprite()
    {
        if (litChandelierSprite == null) return;

        if (chandelierSpriteRenderer != null)
        {
            chandelierSpriteRenderer.sprite = litChandelierSprite;
            if (chandelierSpotLight != null)
            {
                // Ensure both component and GameObject are active
                try { chandelierSpotLight.gameObject.SetActive(true); } catch { }
                chandelierSpotLight.enabled = true;
                chandelierSpotLight.intensity = chandelierLightTargetIntensity;
            }
            return;
        }

        if (chandelierObject != null)
        {
            var sr = chandelierObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = litChandelierSprite;
                if (chandelierSpotLight != null)
                {
                    try { chandelierSpotLight.gameObject.SetActive(true); } catch { }
                    chandelierSpotLight.enabled = true;
                    chandelierSpotLight.intensity = chandelierLightTargetIntensity;
                }
                return;
            }

            var img = chandelierObject.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = litChandelierSprite;
                if (chandelierSpotLight != null)
                {
                    try { chandelierSpotLight.gameObject.SetActive(true); } catch { }
                    chandelierSpotLight.enabled = true;
                    chandelierSpotLight.intensity = chandelierLightTargetIntensity;
                }
                return;
            }
        }
    }
}
