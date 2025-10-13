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
        // A inscrição do evento foi movida para o Start()
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Player.Disable();
            playerControls.Player.OpenInventory.performed -= ToggleInventory;
        }
        // A desinscrição foi movida para o OnDestroy()
    }

    private void Start()
    {
        inventoryPanel.SetActive(false);

        // --- A CORREÇÃO ESTÁ AQUI ---
        // Movemos a inscrição do evento para o Start(), que é garantido de rodar
        // depois que todos os Awakes() (incluindo o do InventoryManager) já terminaram.
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryUI;
        }
        else
        {
            Debug.LogError("InventoryManager.Instance não foi encontrado no Start! Verifique se o objeto existe na cena inicial.");
        }
        
        // Atualiza a UI uma vez no início.
        UpdateInventoryUI();
    }

    // Para objetos persistentes, é mais seguro se desinscrever no OnDestroy.
    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryUI;
        }
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
        // Debug.Log("--- [DEBUG] UpdateInventoryUI foi chamado ---"); // Você pode remover os logs se quiser

        if (InventoryManager.Instance == null) return;

        List<InventoryItem> items = InventoryManager.Instance.GetItems();
        // Debug.Log($"[DEBUG] Itens na lista do inventário: {items.Count}");

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
            Debug.Log($"Inspecionando item: {clickedItem.itemName}");
        }
    }
}

