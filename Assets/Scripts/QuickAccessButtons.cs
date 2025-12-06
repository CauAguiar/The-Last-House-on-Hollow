using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Conecta botões rápidos (Inventory, Journal, Tome) às respectivas UIs
/// e mantém o botão do diário desabilitado até que o jogador possua o item do diário
/// ou tenha coletado ao menos uma página (fallback).
/// </summary>
public class QuickAccessButtons : MonoBehaviour
{
    public static QuickAccessButtons Instance { get; private set; }

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
    public TomeUIManager tomoUI; // manager do Tomo

    private void Start()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

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

        Debug.Log($"QuickAccessButtons: Start() tomoButton={(tomoButton!=null)} tomoUI={(tomoUI!=null)}");
    }

    private void Update()
    {
        // Toggle Tome UI with B if Tome is available
        if (tomoButton != null && tomoButton.interactable)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.bKey.wasPressedThisFrame)
            {
                Debug.Log("QuickAccessButtons: B pressed (detected via Keyboard.current)");
                // If tomoUI is assigned, toggle it; otherwise call the placeholder
                if (tomoUI != null)
                {
                    Debug.Log("QuickAccessButtons: calling tomoUI.Toggle()");
                    tomoUI.Toggle();
                }
                else
                {
                    Debug.LogWarning("QuickAccessButtons: tomoUI is null in Update(); falling back to OnTomeButtonClicked()");
                    OnTomeButtonClicked();
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

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

    /// <summary>
    /// Pulse the journal button to draw attention (scale animation).
    /// Safe to call even if the journal button is disabled; it will still animate the transform.
    /// </summary>
    public void PulseJournalButton(float pulseScale = 1.25f, float pulseDuration = 0.6f)
    {
        if (journalButton == null) return;
        StopCoroutine("PulseCoroutine");
        StartCoroutine(PulseCoroutine(pulseScale, pulseDuration));
    }

    private System.Collections.IEnumerator PulseCoroutine(float targetScale, float duration)
    {
        Transform t = journalButton.transform;
        Vector3 original = t.localScale;
        float half = duration * 0.5f;
        float elapsed = 0f;

        // scale up
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / half));
            t.localScale = Vector3.Lerp(original, original * targetScale, p);
            yield return null;
        }

        // scale down
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / half));
            t.localScale = Vector3.Lerp(original * targetScale, original, p);
            yield return null;
        }

        t.localScale = original;
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
            // Do not open inventory if another UI has blocked input
            if (!inventoryUI.IsInventoryOpen() && UIInputBlocker.IsBlocked)
            {
                return;
            }
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
            // Prevent opening via quick button when another modal UI is blocking input
            if (!journalUI.journalPanel.activeSelf && UIInputBlocker.IsBlocked)
            {
                return;
            }
            journalUI.ToggleJournal();
        }
    }

    private void OnTomeButtonClicked()
    {
        Debug.Log("QuickAccessButtons: OnTomeButtonClicked called");
        if (tomoUI != null)
        {
            // Prevent opening the Tome when another UI is blocking input
            if (!tomoUI.IsTomeOpen() && UIInputBlocker.IsBlocked)
            {
                return;
            }
            Debug.Log("QuickAccessButtons: OnTomeButtonClicked -> tomoUI.Toggle()");
            tomoUI.Toggle();
        }
        else
        {
            Debug.LogWarning("QuickAccessButtons: OnTomeButtonClicked but tomoUI is null");
        }
    }

    /// <summary>
    /// Called externally when the player acquires the Tome item. Enables the Tome quick button.
    /// </summary>
    public void SetTomeAvailable(bool available)
    {
        Debug.Log($"QuickAccessButtons: SetTomeAvailable({available}) called");
        UpdateTomeButtonState(available);
        if (available && tomoButton != null)
        {
            // optional: pulse to draw attention
            StopCoroutine("PulseCoroutine");
            StartCoroutine(PulseCoroutine(1.15f, 0.5f));
        }
    }
}
