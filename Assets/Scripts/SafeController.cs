using UnityEngine;
using System.Collections;
#if UNITY_2020_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#else
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// Interactable que representa o cofre. Ao inspecionar, abre o `SafeUIManager`.
/// Quando a senha correta for inserida, concede um item, desbloqueia a última página
/// do diário e habilita o Tomo.
/// </summary>
public class SafeController : InteractableBase
{
    [Header("Recompensas")]
    [Tooltip("Item que será dado ao jogador ao abrir o cofre (ex: chave)")]
    [SerializeField] private InventoryItem rewardItem;

    [Tooltip("ID da página do diário que será concedida (última página)")]
    [SerializeField] private int diaryPageId = 0;

    [Tooltip("Sprite do cofre aberto (opcional)")]
    [SerializeField] private Sprite openSprite;

    [Header("Estado/Saving")]
    [Tooltip("ID único usado para marcar o cofre como resolvido")]
    [SerializeField] private string uniqueId;

    [Header("Tomo")]
    [Tooltip("Se verdadeiro, quando o cofre for aberto habilita o Tomo na UI de atalhos")]
    [SerializeField] private bool grantsTome = true;

    private bool isOpened = false;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null && GameStateManager.Instance.IsCollected(uniqueId))
        {
            isOpened = true;
            if (spriteRenderer != null && openSprite != null)
            {
                spriteRenderer.sprite = openSprite;
            }
        }
    }

    public override void OnInspect()
    {
        if (isOpened)
        {
            InteractionManager.Instance.ShowDialogue("O cofre está vazio.");
        }
        else
        {
            if (SafeUIManager.Instance != null)
            {
                SafeUIManager.Instance.OpenSafePuzzle(this);
            }
        }
    }

    // Open UI directly when interacting; do not allow context menu and prevent re-open after solved
    public override void Interact()
    {
        if (isOpened) return;
        OnInspect();
    }

    public override bool CanShowContextMenu()
    {
        return false;
    }

    public void OnPuzzleSolved()
    {
        // Called by UI manager when password is correct. Grant rewards and persist.
        isOpened = true;
        if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.MarkAsCollected(uniqueId);
        }

        if (spriteRenderer != null && openSprite != null)
        {
            spriteRenderer.sprite = openSprite;
        }

        if (rewardItem != null)
        {
            InventoryManager.Instance.AddItem(rewardItem);
        }

        if (diaryPageId > 0 && JournalManager.Instance != null)
        {
            JournalManager.Instance.CollectPage(diaryPageId);
        }

        if (grantsTome)
        {
            QuickAccessButtons.Instance?.SetTomeAvailable(true);
        }

        // Trigger lights off in other scenes after a short delay (10s).
        // Use reflection to avoid a hard compile-time dependency on LightManager.
        bool invoked = false;
        // FindObjectsOfType with includeInactive is obsolete on some Unity versions; use Resources.FindObjectsOfTypeAll which returns inactive objects too.
        var monos = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        foreach (var mb in monos)
        {
            if (mb == null) continue;
            var t = mb.GetType();
            if (t.Name == "LightManager")
            {
                var method = t.GetMethod("DisableTrackedLightsAfter", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    // Chamada imediata (sem atraso)
                    method.Invoke(mb, new object[] { 0f, true, 1.5f });
                    invoked = true;
                }
                break;
            }
        }
        if (!invoked)
        {
            Debug.Log("LightManager instance not found; skipping global light disable.");
        }

        // Provide brief dialogue feedback
        InteractionManager.Instance.ShowDialogue("QUE MERDA TA  ACONTECENDO AQUI?");
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    
}
