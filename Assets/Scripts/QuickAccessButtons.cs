using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Conecta botões rápidos (Inventory, Journal, Tome) às respectivas UIs
/// e mantém o botão do diário desabilitado até que o jogador possua o item do diário
/// ou tenha coletado ao menos uma página (fallback).
/// </summary>
public class QuickAccessButtons : MonoBehaviour
{
    [Header("Botões UI")]
    public Button inventoryButton;
    public Button journalButton;
    public Button tomoButton; // reservado para uso futuro
    [Header("Icon Images (optional)")]
    [Tooltip("Imagem do ícone do botão do Diário; será ativada somente quando o diário estiver disponível.")]
    public Image journalIcon;
    [Tooltip("Imagem do ícone do botão do Tomo; reservado para uso futuro.")]
    public Image tomoIcon;

    [Header("Optional Labels (TextMeshPro)")]
    [Tooltip("Label (TMP) do botão do Diário — será ativada junto com o ícone quando o diário estiver disponível.")]
    public TextMeshProUGUI journalLabel;
    [Tooltip("Label (TMP) do botão do Tomo — reservado para uso futuro.")]
    public TextMeshProUGUI tomoLabel;
    [Tooltip("Label (TMP) do botão do Inventário — opcional, caso queira ocultar quando vazio.")]
    public TextMeshProUGUI inventoryLabel;

    [Header("Referências de UI")]
    public InventoryUIController inventoryUI;
    public JournalUIManager journalUI;
    // public TomeUIManager tomoUI; // deixe espaço para depois

    private void Start()
    {
        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(OnInventoryButtonClicked);
        if (journalButton != null)
            journalButton.onClick.AddListener(OnJournalButtonClicked);
        if (tomoButton != null)
            tomoButton.onClick.AddListener(OnTomeButtonClicked);

        // Inicialmente, o botão do diário fica interagível somente se já temos o diário
        UpdateJournalButtonState();

        // Tome starts hidden/unavailable until implemented/collected
        UpdateTomeButtonState(false);

        // Subscrições de inventário para atualizar disponibilidade do diário
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded += HandleItemAdded;
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void OnDestroy()
    {
        if (inventoryButton != null)
            inventoryButton.onClick.RemoveListener(OnInventoryButtonClicked);
        if (journalButton != null)
            journalButton.onClick.RemoveListener(OnJournalButtonClicked);
        if (tomoButton != null)
            tomoButton.onClick.RemoveListener(OnTomeButtonClicked);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded -= HandleItemAdded;
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleItemAdded(InventoryItem item)
    {
        UpdateJournalButtonState();
    }

    private void HandleInventoryChanged()
    {
        UpdateJournalButtonState();
    }

    private void UpdateJournalButtonState()
    {
        if (journalButton == null) return;

        bool enabled = false;

        var jm = JournalManager.Instance;
        if (jm == null)
        {
            // try to locate JournalManager in scene if the singleton wasn't set for some reason
#if UNITY_2023_1_OR_NEWER
            jm = UnityEngine.Object.FindFirstObjectByType<JournalManager>();
#else
            jm = UnityEngine.Object.FindObjectOfType<JournalManager>();
#endif
        }
        if (jm != null)
        {
            // Preferência: se um InventoryItem de "diário" foi configurado no JournalManager,
            // só habilitar quando o jogador tiver esse item.
            if (jm.journalItemReference != null && InventoryManager.Instance != null)
            {
                enabled = InventoryManager.Instance.HasItem(jm.journalItemReference);
            }
            else
            {
                // Fallback: habilita se o jogador já coletou pelo menos uma página
                enabled = (jm.collectedPages != null && jm.collectedPages.Count > 0);
            }
        }

        journalButton.interactable = enabled;

        // Atualiza também a visibilidade do ícone (se atribuído). O ícone só aparece quando o diário estiver disponível.
        if (journalIcon != null)
        {
            journalIcon.enabled = enabled;
        }
        else if (journalButton.image != null)
        {
            // Fallback: use a imagem do próprio Button
            journalButton.image.enabled = enabled;
        }

        // Atualiza label TMP: prefer explicit, senão procura um child TMP e ativa/desativa
        if (journalLabel != null)
        {
            journalLabel.enabled = enabled;
        }
        else
        {
            var tmp = journalButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.enabled = enabled;
        }
    }

    private void UpdateTomeButtonState(bool available)
    {
        if (tomoButton == null) return;
        tomoButton.interactable = available;

        if (tomoIcon != null)
        {
            tomoIcon.enabled = available;
        }
        else if (tomoButton.image != null)
        {
            tomoButton.image.enabled = available;
        }

        if (tomoLabel != null)
        {
            tomoLabel.enabled = available;
        }
        else
        {
            var tmp = tomoButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.enabled = available;
        }
    }

    private void OnInventoryButtonClicked()
    {
        if (inventoryUI == null && InventoryUIController.Instance != null)
            inventoryUI = InventoryUIController.Instance;

        if (inventoryUI != null)
        {
            if (inventoryUI.IsInventoryOpen())
                inventoryUI.CloseInventory();
            else
                inventoryUI.OpenInventoryForUse(null);
        }
    }

    private void OnJournalButtonClicked()
    {
        if (journalUI == null)
    #if UNITY_2023_1_OR_NEWER
            journalUI = UnityEngine.Object.FindFirstObjectByType<JournalUIManager>();
    #else
            journalUI = UnityEngine.Object.FindObjectOfType<JournalUIManager>();
    #endif

        if (journalUI != null)
        {
            journalUI.ToggleJournal();
        }
    }

    private void OnTomeButtonClicked()
    {
        // Espaço reservado: abra o UI do tomo quando for implementado.
        Debug.Log("Tome button pressed: Tome UI not implemented yet.");
    }
}
