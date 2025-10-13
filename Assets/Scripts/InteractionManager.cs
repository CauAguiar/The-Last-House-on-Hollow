using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("Componentes da UI")]
    [SerializeField] private GameObject contextMenu;
    [SerializeField] private Button inspectButton;
    [SerializeField] private Button useItemButton;

    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI dialogueText;

    private InteractableBase currentInteractable;

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
        }
    }

    private void Start()
    {
        contextMenu.SetActive(false);
        dialogueBox.SetActive(false);

        inspectButton.onClick.AddListener(OnInspectClicked);
        useItemButton.onClick.AddListener(OnUseItemClicked);
    }

    public void ShowContextMenu(InteractableBase interactable, Vector2 position)
    {
        currentInteractable = interactable;
        contextMenu.transform.position = position;
        contextMenu.SetActive(true);
    }

    public void ShowDialogue(string text)
    {
        dialogueText.text = text;
        dialogueBox.SetActive(true);
    }

    private void OnInspectClicked()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnInspect();
        }
        contextMenu.SetActive(false);
    }

    private void OnUseItemClicked()
    {
        if (currentInteractable != null)
        {
            InventoryUIController.Instance.OpenInventoryForUse(currentInteractable);
        }
        contextMenu.SetActive(false);
    }
}
