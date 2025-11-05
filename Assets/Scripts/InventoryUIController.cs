using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventoryUIController : MonoBehaviour
{
    public static InventoryUIController Instance { get; private set; }

    [Header("Componentes da UI")]
    public GameObject inventoryPanel;
    public List<Button> inventorySlots;

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
        inventoryPanel.SetActive(false);

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
        inventoryPanel.SetActive(isInventoryOpen);
        if (!isInventoryOpen)
        {
            currentUseTarget = null;
        }
    }

    public void OpenInventoryForUse(InteractableBase target)
    {
        currentUseTarget = target;
        isInventoryOpen = true;
        inventoryPanel.SetActive(true);
    }

    private void UpdateInventoryUI()
    {
        if (InventoryManager.Instance == null) return;

        List<InventoryItem> items = InventoryManager.Instance.GetItems();

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            inventorySlots[i].onClick.RemoveAllListeners();

            if (i < items.Count)
            {
                InventoryItem currentItem = items[i];
                
                if (currentItem.icon != null)
                {
                    inventorySlots[i].image.sprite = currentItem.icon;
                    inventorySlots[i].image.enabled = true;
                }
                else
                {
                    inventorySlots[i].image.enabled = false;
                }
                
                inventorySlots[i].onClick.AddListener(() => OnSlotClicked(currentItem));
            }
            else
            {
                inventorySlots[i].image.sprite = null;
                inventorySlots[i].image.enabled = false;
            }
        }
    }

    private void OnSlotClicked(InventoryItem clickedItem)
    {
        if (currentUseTarget != null)
        {
            currentUseTarget.OnUseItem(clickedItem);
            currentUseTarget = null;
            isInventoryOpen = false;
            inventoryPanel.SetActive(false);
        }
        else
        {
            // Abre o painel de inspeção do item
            if (ItemInspectionController.Instance != null)
            {
                ItemInspectionController.Instance.ShowInspection(clickedItem);
            }
            else
            {
                Debug.LogError("ItemInspectionController.Instance não foi encontrado!");
            }
        }
    }
}

