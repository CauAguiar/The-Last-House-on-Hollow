using UnityEngine;
using UnityEngine.UI;

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
            jm = UnityEngine.Object.FindObjectOfType<JournalManager>();
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
            journalUI = UnityEngine.Object.FindObjectOfType<JournalUIManager>();

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
