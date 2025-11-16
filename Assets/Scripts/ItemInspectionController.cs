using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Controlador do painel de inspeção de itens do inventário.
/// Exibe informações detalhadas sobre um item selecionado.
/// </summary>
public class ItemInspectionController : MonoBehaviour
{
    public static ItemInspectionController Instance { get; private set; }

    [Header("UI do Painel de Inspeção")]
    [Tooltip("Painel principal da tela de inspeção")]
    public GameObject inspectionPanel;

    [Tooltip("Imagem que exibe o ícone do item")]
    public Image itemIconImage;

    [Tooltip("Texto que exibe o nome do item")]
    public TextMeshProUGUI itemNameText;

    [Tooltip("Texto que exibe a descrição do item")]
    public TextMeshProUGUI itemDescriptionText;

    [Tooltip("Botão para fechar o painel de inspeção")]
    public Button closeButton;

    private InventoryItem currentInspectedItem;

    private void Awake()
    {
        // Padrão Singleton
        if (Instance == null)
        {
            Instance = this;
            // Nota: não marcamos como DontDestroyOnLoad para evitar que referências de UI da cena fiquem inválidas
            // quando a cena for trocada. O ItemInspectionController deve ser configurado por cena.
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Inicialmente o painel deve estar desativado
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(false);
        }

        // Configura o listener do botão de fechar
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInspection);
        }
        else
        {
            Debug.LogWarning("ItemInspectionController: closeButton não está atribuído na cena. Verifique o prefab/UI.");
        }
    }

    /// <summary>
    /// Abre o painel de inspeção e exibe as informações do item.
    /// </summary>
    /// <param name="item">O item a ser inspecionado</param>
    public void ShowInspection(InventoryItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("Tentativa de inspecionar um item nulo!");
            return;
        }

        currentInspectedItem = item;

        // Popula os campos da UI com os dados do item
        if (itemIconImage != null)
        {
            if (item.icon != null)
            {
                itemIconImage.sprite = item.icon;
                itemIconImage.enabled = true;
            }
            else
            {
                itemIconImage.enabled = false;
            }
        }

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.description;
        }

        // Ativa o painel
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(true);
        }

        // Ajustes defensivos: desativa raycastTarget de imagens totalmente transparentes
        // e registra diagnósticos sobre sobreposição de elementos UI com os slots do inventário.
        TryFixTransparentRaycastTargets();
        LogInventorySlotOverlaps();

        // Pausa o movimento do jogador (opcional mas recomendado)
        PausePlayerMovement(true);
        UIInputBlocker.Block("ItemInspection");
        GamePauseManager.Pause("ItemInspection");
    }

    private void TryFixTransparentRaycastTargets()
    {
        if (inspectionPanel == null) return;
        var images = inspectionPanel.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img == null) continue;
            // If image is essentially invisible (no sprite and near-zero alpha) it should not block raycasts
            bool transparent = (img.sprite == null && img.color.a <= 0.01f) || img.color.a <= 0.01f;
            if (transparent && img.raycastTarget)
            {
                img.raycastTarget = false;
                Debug.Log($"ItemInspectionController: disabled raycastTarget on transparent image '{img.gameObject.name}'");
            }
        }
    }

    private void LogInventorySlotOverlaps()
    {
        if (inspectionPanel == null) return;
        if (InventoryUIController.Instance == null || InventoryUIController.Instance.inventorySlots == null) return;

        RectTransform panelRt = inspectionPanel.GetComponent<RectTransform>();
        if (panelRt == null) return;

        Rect panelRect = GetScreenRect(panelRt);

        for (int i = 0; i < InventoryUIController.Instance.inventorySlots.Count; i++)
        {
            var btn = InventoryUIController.Instance.inventorySlots[i];
            if (btn == null) continue;
            var slotRt = btn.GetComponent<RectTransform>();
            Rect slotRect = GetScreenRect(slotRt);

            if (slotRect.Overlaps(panelRect))
            {
                Debug.Log($"ItemInspectionController: inventory slot index {i} ('{btn.gameObject.name}') overlaps inspectionPanel rect — this can block clicks.");
            }

            // Also check individual images inside the panel
            var images = inspectionPanel.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img == null) continue;
                Rect imgRect = GetScreenRect(img.GetComponent<RectTransform>());
                if (slotRect.Overlaps(imgRect))
                {
                    Debug.Log($"ItemInspectionController: slot index {i} ('{btn.gameObject.name}') overlaps image '{img.gameObject.name}' (raycastTarget={img.raycastTarget}).");
                }
            }
        }
    }

    private Rect GetScreenRect(RectTransform rt)
    {
        if (rt == null) return new Rect();
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        // Convert world corners to screen space
        Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }

    /// <summary>
    /// Fecha o painel de inspeção e retorna ao inventário ou ao jogo.
    /// </summary>
    public void CloseInspection()
    {
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(false);
        }

        currentInspectedItem = null;

        // Despausa o movimento do jogador
        PausePlayerMovement(false);
        UIInputBlocker.Unblock("ItemInspection");
        Debug.Log($"ItemInspectionController: CloseInspection called. UIInputBlocker.IsBlocked={UIInputBlocker.IsBlocked}");
        GamePauseManager.Unpause("ItemInspection");
    }

    /// <summary>
    /// Pausa ou despausa o movimento do jogador.
    /// </summary>
    /// <param name="pause">True para pausar, False para despausar</param>
    private void PausePlayerMovement(bool pause)
    {
        // Desabilita/Habilita o input do jogador através do PlayerControls
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.enabled = !pause;
        }

        // Alternativamente, poderia ser usado Time.timeScale = 0 para pausar completamente o jogo
        // mas isso pode afetar animações e outras mecânicas que não queremos pausar
        // Time.timeScale = pause ? 0f : 1f;
    }

    /// <summary>
    /// Retorna se o painel de inspeção está atualmente ativo.
    /// </summary>
    /// <returns>True se o painel estiver ativo, False caso contrário</returns>
    public bool IsInspectionOpen()
    {
        return inspectionPanel != null && inspectionPanel.activeSelf;
    }

    private void OnDestroy()
    {
        // Remove o listener ao destruir
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseInspection);
        }
    }
}
