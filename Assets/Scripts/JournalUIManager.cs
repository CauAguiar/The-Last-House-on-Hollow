using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class JournalUIManager : MonoBehaviour
{
    [Header("Referências de UI")]
    public GameObject journalPanel;
    public TMP_Text pageText;
    public Button nextButton;
    public Button prevButton;
    [Tooltip("Botão para fechar o diário (opcional).")]
    public Button closeButton;

    [Header("Controle interno")]
    private int currentPageIndex = 0;

    [Header("Áudio")]
    [Tooltip("Nome do som no SoundBank para avançar a página")]
    public string nextPageSfx;
    [Tooltip("Nome do som no SoundBank para voltar a página")]
    public string prevPageSfx;
    public AudioManager.Category sfxCategory = AudioManager.Category.UI;

    private void Start()
    {
        journalPanel.SetActive(false);
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PreviousPage);
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => ToggleJournal());
        }

        if (journalPanel != null && journalPanel.GetComponent<UIAutoCloseOnCancel>() == null)
        {
            var helper = journalPanel.AddComponent<UIAutoCloseOnCancel>();
            helper.panel = journalPanel;
            helper.closeButton = closeButton;
        }

        if (journalPanel != null && journalPanel.GetComponent<UIFocusTrap>() == null)
        {
            journalPanel.AddComponent<UIFocusTrap>();
        }
    }

    private void OnEnable()
    {
        if (JournalManager.Instance != null)
            JournalManager.Instance.OnPageCollected += HandlePageCollected;
    }

    private void OnDisable()
    {
        if (JournalManager.Instance != null)
            JournalManager.Instance.OnPageCollected -= HandlePageCollected;
    }

    private void HandlePageCollected(int pageId)
    {
        // HandlePageCollected: log removed
        // If the journal is open, refresh the displayed page list and show the newly collected page
        if (journalPanel != null && journalPanel.activeSelf)
        {
            var jm = JournalManager.Instance;
            if (jm == null) return;
            // find new page index
            int idx = jm.collectedPages.IndexOf(pageId);
            if (idx >= 0)
            {
                currentPageIndex = idx;
                ShowPage(currentPageIndex);
            }
            else
            {
                // JournalUIManager: pageId not found in collectedPages after collect. (log removed)
            }
        }
    }

    private void Update()
    {
        if (JournalManager.Instance == null || JournalManager.Instance.collectedPages.Count == 0)
        {
            return;
        }

        bool openPressed = false;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
            openPressed = true;
#else
        if (Input.GetKeyDown(KeyCode.J))
            openPressed = true;
#endif

        if (openPressed)
        {
            // If any UI has blocked input, don't open the journal from shortcut
            if (!journalPanel.activeSelf && UIInputBlocker.IsBlocked)
            {
                // Ignora atalho enquanto outra UI modal está aberta
            }
            else
            {
                ToggleJournal();
            }
        }

        // Diagnostic: when left mouse clicked, log which UI element is under pointer
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            // UI pointer diagnostic log removed
        }
#else
        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            LogUiUnderPointer();
        }
#endif
    }

    private void LogUiUnderPointer()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        string selected = es != null && es.currentSelectedGameObject != null ? es.currentSelectedGameObject.name : "<none>";
        // Mouse click diagnostic log removed

        // Raycast against UI
        var pointerData = new UnityEngine.EventSystems.PointerEventData(es);
        // Use the active input system to get pointer position when available
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (UnityEngine.InputSystem.Mouse.current != null)
            pointerData.position = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        else
            pointerData.position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#else
        pointerData.position = UnityEngine.Input.mousePosition;
#endif
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        var gr = journalPanel != null ? journalPanel.GetComponentInParent<UnityEngine.Canvas>()?.GetComponent<UnityEngine.UI.GraphicRaycaster>() : null;
        if (gr == null)
        {
            // try to find any GraphicRaycaster in scene
#if UNITY_2023_1_OR_NEWER
            gr = UnityEngine.Object.FindFirstObjectByType<UnityEngine.UI.GraphicRaycaster>();
#else
            gr = UnityEngine.Object.FindObjectOfType<UnityEngine.UI.GraphicRaycaster>();
#endif
        }
            if (gr != null)
        {
            gr.Raycast(pointerData, results);
            // UI raycast diagnostics suppressed
        }
        else
        {
            // JournalUIManager: No GraphicRaycaster found for UI raycast diagnostics. (log removed)
        }
    }

// Dentro de JournalUIManager.cs

public void ToggleJournal()
{
    bool isActive = !journalPanel.activeSelf;

    // If opening, notify exclusive manager to close other panels first
    if (isActive)
    {
        // Ensure exclusivity (create manager if missing)
        UIExclusiveManager.GetOrCreate().PanelOpening(journalPanel);
    }

    journalPanel.SetActive(isActive);

    if (isActive)
    {
        currentPageIndex = 0;
        ShowPage(currentPageIndex);
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.LockMovement();
        UIInputBlocker.Block("Journal");
        GamePauseManager.Pause("Journal");
        if (UnityEngine.EventSystems.EventSystem.current != null && closeButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
    }
    else
    {
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.UnlockMovement();
        UIInputBlocker.Unblock("Journal");
        GamePauseManager.Unpause("Journal");
    }
}

// Called by UIExclusiveManager when another panel causes this one to close
private void OnExclusivePanelClosed()
{
    // Ensure cleanup steps are applied when the panel is closed externally
    if (journalPanel != null && !journalPanel.activeSelf)
    {
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.UnlockMovement();
        UIInputBlocker.Unblock("Journal");
        GamePauseManager.Unpause("Journal");
    }
}

    private void ShowPage(int index)
    {
        var jm = JournalManager.Instance;

        // ShowPage called (log removed)
        if (jm == null)
        {
            // JournalUIManager.ShowPage: JournalManager.Instance == null (log removed)
            pageText.text = "Nenhuma página coletada ainda.";
            UpdateButtonStates();
            return;
        }

        if (jm.collectedPages == null || jm.collectedPages.Count == 0)
        {
            pageText.text = "Nenhuma página coletada ainda.";
            UpdateButtonStates();
            return;
        }

        if (index < 0 || index >= jm.collectedPages.Count)
        {
            // JournalUIManager.ShowPage: index out of range (log removed)
            pageText.text = "Página não encontrada.";
            UpdateButtonStates();
            return;
        }

        int pageId = jm.collectedPages[index];
        // Displaying page (log removed)
        string content = jm.GetPageContent(pageId);
        pageText.text = content;

        UpdateButtonStates();
    }

    public void NextPage()
    {
        var jm = JournalManager.Instance;
        if (jm == null || jm.collectedPages.Count == 0) return;

        // NextPage called (log removed)
        if (currentPageIndex < jm.collectedPages.Count - 1)
        {
            currentPageIndex++;
            ShowPage(currentPageIndex);
            // Toca som de avanço
            if (!string.IsNullOrEmpty(nextPageSfx) && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(nextPageSfx, sfxCategory);
            }
        }
    }

    public void PreviousPage()
    {
        var jm = JournalManager.Instance;
        if (jm == null || jm.collectedPages.Count == 0) return;

        // PreviousPage called (log removed)
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowPage(currentPageIndex);
            // Toca som de voltar
            if (!string.IsNullOrEmpty(prevPageSfx) && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(prevPageSfx, sfxCategory);
            }
        }
    }

    private void UpdateButtonStates()
    {
        var jm = JournalManager.Instance;
        if (jm == null || jm.collectedPages.Count == 0) return;

        prevButton.interactable = (currentPageIndex > 0);
        nextButton.interactable = (currentPageIndex < jm.collectedPages.Count - 1);
        // UpdateButtonStates: diagnostic log removed
    }
}