using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Controla a interface visual do inventário (UI).
/// É um Singleton persistente responsável por abrir/fechar o painel e atualizar os slots.
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    public static InventoryUIController Instance { get; private set; }

    [Header("Componentes da UI")]
    [Tooltip("O GameObject do painel principal que contém todos os slots do inventário.")]
    public GameObject inventoryPanel;

    [Tooltip("A lista de componentes Image que servem como slots. Preencha no Inspector.")]
    public List<Image> inventorySlots;

    private PlayerControls playerControls;
    private bool isInventoryOpen = false;

    private void Awake()
    {
        // Lógica do Singleton para garantir que só exista uma UI de inventário no jogo.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Torna este objeto (e seu Canvas filho) persistente
        }
        else
        {
            Destroy(gameObject); // Destrói qualquer duplicata que apareça ao recarregar a cena inicial
            return;
        }

        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls.Player.Enable();
        playerControls.Player.OpenInventory.performed += ToggleInventory;

        // A checagem se a instância existe agora é mais segura
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryUI;
        }
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
        playerControls.Player.OpenInventory.performed -= ToggleInventory;

        // Checa se a instância ainda existe antes de tentar remover o listener
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryUI;
        }
    }

    private void Start()
    {
        inventoryPanel.SetActive(false);
        UpdateInventoryUI();
    }

    private void ToggleInventory(InputAction.CallbackContext context)
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryPanel.SetActive(isInventoryOpen);
    }

    private void UpdateInventoryUI()
    {
        // Garante que o InventoryManager já foi inicializado
        if (InventoryManager.Instance == null) return;

        List<InventoryItem> items = InventoryManager.Instance.GetItems();

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (i < items.Count)
            {
                inventorySlots[i].sprite = items[i].icon;
                inventorySlots[i].enabled = true;
            }
            else
            {
                inventorySlots[i].sprite = null;
                inventorySlots[i].enabled = false;
            }
        }
    }
}

