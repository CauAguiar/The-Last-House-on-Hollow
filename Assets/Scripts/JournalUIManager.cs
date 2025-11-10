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

    [Header("Controle interno")]
    private int currentPageIndex = 0;

    private void Start()
    {
        journalPanel.SetActive(false);
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PreviousPage);
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
            ToggleJournal();
    }

// Dentro de JournalUIManager.cs

public void ToggleJournal()
{
    bool isActive = !journalPanel.activeSelf;
    journalPanel.SetActive(isActive);

    if (isActive)
    {
        currentPageIndex = 0;
        ShowPage(currentPageIndex);
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.LockMovement();
        UIInputBlocker.Block("Journal");
        GamePauseManager.Pause("Journal");
    }
    else
    {
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.UnlockMovement();
        UIInputBlocker.Unblock("Journal");
        GamePauseManager.Unpause("Journal");
    }
}

    private void ShowPage(int index)
    {
        var jm = JournalManager.Instance;

        if (jm != null && jm.collectedPages.Count > 0 && index < jm.collectedPages.Count)
        {
            int pageId = jm.collectedPages[index];
            string content = jm.GetPageContent(pageId);
            pageText.text = content;
        }
        else
        {
            pageText.text = "Nenhuma página coletada ainda.";
        }

        UpdateButtonStates();
    }

    public void NextPage()
    {
        var jm = JournalManager.Instance;
        if (jm == null || jm.collectedPages.Count == 0) return;

        if (currentPageIndex < jm.collectedPages.Count - 1)
        {
            currentPageIndex++;
            ShowPage(currentPageIndex);
        }
    }

    public void PreviousPage()
    {
        var jm = JournalManager.Instance;
        if (jm == null || jm.collectedPages.Count == 0) return;

        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowPage(currentPageIndex);
        }
    }

    private void UpdateButtonStates()
    {
        var jm = JournalManager.Instance;
        if (jm == null || jm.collectedPages.Count == 0) return;

        prevButton.interactable = (currentPageIndex > 0);
        nextButton.interactable = (currentPageIndex < jm.collectedPages.Count - 1);
    }
}