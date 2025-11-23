using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryUIController : MonoBehaviour
{
    public static InventoryUIController Instance { get; private set; }

    [Header("Componentes da UI")]
    public GameObject inventoryPanel;
    public List<Button> inventorySlots;
    [Tooltip("Botão para fechar o painel do inventário (opcional).")]
    public Button closeButton;

    [Header("Inspection UI (integrado)")]
    [Tooltip("Imagem do painel de inspeção integrada ao inventário")]
    public Image inspectionIconImage;
    [Tooltip("Nome do item exibido ao passar o mouse")]
    public TextMeshProUGUI inspectionNameText;
    [Tooltip("Descrição do item exibida ao passar o mouse")]
    public TextMeshProUGUI inspectionDescriptionText;

    private PlayerControls playerControls;
    private bool isInventoryOpen = false;
    private InteractableBase currentUseTarget;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls.Player.Enable();
        playerControls.Player.OpenInventory.performed += ToggleInventory;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryUI;
        }
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Player.Disable();
            playerControls.Player.OpenInventory.performed -= ToggleInventory;
        }
    }

    private void Start()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryUI;
        }
        else
        {
            Debug.LogError("InventoryManager.Instance não foi encontrado no Start!");
        }

        UpdateInventoryUI();

        // Ensure all slot images and inspection UI start disabled until items/inspection occur
        if (inventorySlots != null)
        {
            foreach (var slot in inventorySlots)
            {
                if (slot == null) continue;
                if (slot.image != null)
                {
                    slot.image.sprite = null;
                    slot.image.enabled = false;
                }
            }
        }

        if (inspectionIconImage != null)
        {
            inspectionIconImage.sprite = null;
            inspectionIconImage.enabled = false;
        }
        if (inspectionNameText != null)
        {
            inspectionNameText.text = "";
            inspectionNameText.enabled = false;
        }
        if (inspectionDescriptionText != null)
        {
            inspectionDescriptionText.text = "";
            inspectionDescriptionText.enabled = false;
        }

        // Auto-attach a small helper so ESC/CANCEL will close the panel consistently
        if (inventoryPanel != null && inventoryPanel.GetComponent<UIAutoCloseOnCancel>() == null)
        {
            var helper = inventoryPanel.AddComponent<UIAutoCloseOnCancel>();
            helper.panel = inventoryPanel;
            helper.closeButton = closeButton;
        }

        // Add focus trap to keep keyboard tab cycling inside the inventory when open
        if (inventoryPanel != null && inventoryPanel.GetComponent<UIFocusTrap>() == null)
        {
            inventoryPanel.AddComponent<UIFocusTrap>();
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryUI;
        }
    }

    /// <summary>
    /// Retorna se o painel do inventário está atualmente ativo na tela.
    /// </summary>
    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }

    private void ToggleInventory(InputAction.CallbackContext context)
    {
        isInventoryOpen = !isInventoryOpen;
        if (inventoryPanel != null)
        {
            if (isInventoryOpen)
            {
                // Ensure exclusive UI: notify manager to close other panels (create if missing)
                UIExclusiveManager.GetOrCreate().PanelOpening(inventoryPanel);
            }
            inventoryPanel.SetActive(isInventoryOpen);
        }

        if (isInventoryOpen)
        {
            UIInputBlocker.Block("Inventory");
            GamePauseManager.Pause("Inventory");
        }
        else
        {
            UIInputBlocker.Unblock("Inventory");
            GamePauseManager.Unpause("Inventory");
        }
        if (!isInventoryOpen)
        {
            currentUseTarget = null;
        }
        else
        {
            // Set keyboard/controller focus to the close button if possible for easy ESC/UI navigation
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                if (closeButton != null && closeButton.gameObject.activeInHierarchy)
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
                }
                else if (inventorySlots != null && inventorySlots.Count > 0 && inventorySlots[0] != null)
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(inventorySlots[0].gameObject);
                }
            }
        }
    }

    public void OpenInventoryForUse(InteractableBase target)
    {
        currentUseTarget = target;
        isInventoryOpen = true;
        if (inventoryPanel != null)
        {
            // Ensure exclusive UI: notify manager to close other panels (create manager if missing)
            UIExclusiveManager.GetOrCreate().PanelOpening(inventoryPanel);
            inventoryPanel.SetActive(true);
        }
        UIInputBlocker.Block("Inventory");
        GamePauseManager.Pause("Inventory");
    }

    public void CloseInventory()
    {
        currentUseTarget = null;
        isInventoryOpen = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        UIExclusiveManager.GetOrCreate().PanelClosed(inventoryPanel);
        UIInputBlocker.Unblock("Inventory");
        GamePauseManager.Unpause("Inventory");
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
        // Remove focus trap when closed
        if (inventoryPanel != null)
        {
            var trap = inventoryPanel.GetComponent<UIFocusTrap>();
            if (trap != null) Destroy(trap);
        }
    }
    
    // Called by UIExclusiveManager when another panel forces this one to close
    private void OnExclusivePanelClosed()
    {
        // If we were open, ensure we cleanup the same as CloseInventory
        if (!isInventoryOpen) return;
        currentUseTarget = null;
        isInventoryOpen = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        UIInputBlocker.Unblock("Inventory");
        GamePauseManager.Unpause("Inventory");
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
        if (inventoryPanel != null)
        {
            var trap = inventoryPanel.GetComponent<UIFocusTrap>();
            if (trap != null) Destroy(trap);
        }
    }

    private void UpdateInventoryUI()
    {
        if (InventoryManager.Instance == null) return;

        List<InventoryItem> items = InventoryManager.Instance.GetItems();

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            var slot = inventorySlots[i];
            if (slot == null) continue;

            slot.onClick.RemoveAllListeners();

            // Ensure EventTrigger exists and clear previous entries
            var existingTrigger = slot.GetComponent<EventTrigger>();
            if (existingTrigger != null)
            {
                existingTrigger.triggers = new List<EventTrigger.Entry>();
            }

            if (i < items.Count)
            {
                InventoryItem currentItem = items[i];

                if (currentItem.icon != null)
                {
                    slot.image.sprite = currentItem.icon;
                    slot.image.enabled = true;
                }
                else
                {
                    slot.image.enabled = false;
                }

                int capturedIndex = i;
                slot.onClick.AddListener(() => OnSlotClicked(currentItem, capturedIndex));
                AddPointerEvents(slot, capturedIndex);
            }
            else
            {
                slot.image.sprite = null;
                slot.image.enabled = false;
                AddPointerEvents(slot, i);
            }
        }
    }

    private void AddPointerEvents(Button btn, int index)
    {
        if (btn == null) return;
        EventTrigger trigger = btn.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        // PointerEnter
        var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback = new EventTrigger.TriggerEvent();
        entryEnter.callback.AddListener((data) => { OnSlotPointerEnter(index); });
        trigger.triggers.Add(entryEnter);

        // PointerExit
        var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback = new EventTrigger.TriggerEvent();
        entryExit.callback.AddListener((data) => { OnSlotPointerExit(index); });
        trigger.triggers.Add(entryExit);
    }

    private void OnSlotPointerEnter(int slotIndex)
    {
        if (InventoryManager.Instance == null) return;
        var items = InventoryManager.Instance.GetItems();
        if (slotIndex < 0 || slotIndex >= items.Count)
        {
            ClearInspectionFields();
            return;
        }

        var item = items[slotIndex];
        if (inspectionIconImage != null)
        {
            if (item.icon != null)
            {
                inspectionIconImage.sprite = item.icon;
                inspectionIconImage.enabled = true;
            }
            else
            {
                inspectionIconImage.enabled = false;
            }
        }
        if (inspectionNameText != null)
        {
            inspectionNameText.text = item.itemName;
            inspectionNameText.enabled = true;
        }
        if (inspectionDescriptionText != null)
        {
            inspectionDescriptionText.text = item.description;
            inspectionDescriptionText.enabled = true;
        }
        // Debug: hover slot log removed to reduce console noise
    }

    private void OnSlotPointerExit(int slotIndex)
    {
        ClearInspectionFields();
        // Debug: exit hover log removed
    }

    private void ClearInspectionFields()
    {
        if (inspectionIconImage != null)
        {
            inspectionIconImage.sprite = null;
            inspectionIconImage.enabled = false;
        }
        if (inspectionNameText != null)
        {
            inspectionNameText.text = "";
            inspectionNameText.enabled = false;
        }
        if (inspectionDescriptionText != null)
        {
            inspectionDescriptionText.text = "";
            inspectionDescriptionText.enabled = false;
        }
    }

    private void OnSlotClicked(InventoryItem clickedItem, int slotIndex)
    {
        // Slot clicked
        if (currentUseTarget != null)
        {
            currentUseTarget.OnUseItem(clickedItem);
            currentUseTarget = null;
            isInventoryOpen = false;
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            UIInputBlocker.Unblock("Inventory");
            GamePauseManager.Unpause("Inventory");
        }
        else
        {
            // Inspection is handled on hover now; clicking without a use target is a no-op
            // Click with no use target — inspection is shown on hover now. (log removed)
        }
    }
}

