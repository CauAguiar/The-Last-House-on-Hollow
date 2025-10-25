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
        // O diário começa fechado
        journalPanel.SetActive(false);

        // Configura botões
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PreviousPage);
    }

    private void Update()
    {
        bool openPressed = false;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // Input System: verifica tecla J no teclado
        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
            openPressed = true;
#else
        // Input Manager (antigo): verifica GetKeyDown
        if (Input.GetKeyDown(KeyCode.J))
            openPressed = true;
#endif

        if (openPressed)
            ToggleJournal();
    }
    public void ToggleJournal()
    {
        bool isActive = !journalPanel.activeSelf;
        journalPanel.SetActive(isActive);

        if (isActive)
        {
            ShowPage(currentPageIndex);
        }
    }

    private void ShowPage(int index)
    {
        var jm = JournalManager.Instance;

        if (jm != null && jm.collectedPages.Count > 0)
        {
            int pageId = jm.collectedPages[index];
            string content = jm.GetPageContent(pageId);
            pageText.text = content;
        }
        else
        {
            pageText.text = "Nenhuma página coletada ainda.";
        }
    }

    public void NextPage()
    {
        var jm = JournalManager.Instance;

        if (jm == null || jm.collectedPages.Count == 0) return;

        currentPageIndex = (currentPageIndex + 1) % jm.collectedPages.Count;
        ShowPage(currentPageIndex);
    }

    public void PreviousPage()
    {
        var jm = JournalManager.Instance;

        if (jm == null || jm.collectedPages.Count == 0) return;

        currentPageIndex--;
        if (currentPageIndex < 0)
            currentPageIndex = jm.collectedPages.Count - 1;

        ShowPage(currentPageIndex);
    }
}