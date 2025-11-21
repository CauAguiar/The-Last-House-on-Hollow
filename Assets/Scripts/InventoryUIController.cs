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
        if (inventoryPanel != null) inventoryPanel.SetActive(isInventoryOpen);
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
    }

    public void OpenInventoryForUse(InteractableBase target)
    {
        currentUseTarget = target;
        isInventoryOpen = true;
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        UIInputBlocker.Block("Inventory");
        GamePauseManager.Pause("Inventory");
    }

    public void CloseInventory()
    {
        currentUseTarget = null;
        isInventoryOpen = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        UIInputBlocker.Unblock("Inventory");
        GamePauseManager.Unpause("Inventory");
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
        if (inspectionNameText != null) inspectionNameText.text = item.itemName;
        if (inspectionDescriptionText != null) inspectionDescriptionText.text = item.description;
        Debug.Log($"InventoryUI: Hover slot {slotIndex} -> '{item.itemName}'");
    }

    private void OnSlotPointerExit(int slotIndex)
    {
        ClearInspectionFields();
        Debug.Log($"InventoryUI: Exit hover slot {slotIndex}");
    }

    private void ClearInspectionFields()
    {
        if (inspectionIconImage != null) { inspectionIconImage.sprite = null; inspectionIconImage.enabled = false; }
        if (inspectionNameText != null) inspectionNameText.text = "";
        if (inspectionDescriptionText != null) inspectionDescriptionText.text = "";
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
            Debug.Log("InventoryUI: click with no use target — inspection is shown on hover now.");
        }
    }
}

