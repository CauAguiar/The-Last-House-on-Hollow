using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Gerencia a exibição de UIs de interação, como o menu de contexto e caixas de diálogo.
/// </summary>
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("Componentes do Menu de Contexto")]
    [SerializeField] private GameObject contextMenu;
    [SerializeField] private Button inspectButton;
    [SerializeField] private Button useItemButton;

    [Header("Componentes da Caixa de Diálogo")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float typingSpeed = 0.04f;

    private InteractableBase currentInteractable;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string fullDialogueText;
    private PlayerControls playerControls;

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
        playerControls.Player.Interact.performed += OnInteractPerformed; 
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Player.Disable();
            playerControls.Player.Interact.performed -= OnInteractPerformed;
        }
    }

    private void Start()
    {
        contextMenu.SetActive(false);
        dialogueBox.SetActive(false);

        inspectButton.onClick.AddListener(OnInspectClicked);
        useItemButton.onClick.AddListener(OnUseItemClicked);
    }
    
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Prioridade 1: Se a caixa de diálogo está ativa, o clique é para ela.
        if (dialogueBox.activeSelf)
        {
            HandleDialogueClick();
            return;
        }
        
        // Prioridade 2: Se o inventário ou o menu de contexto estiverem abertos,
        // o clique será tratado pelo sistema de UI (botões). Ignoramos a interação com o mundo.
        if (contextMenu.activeSelf || (InventoryUIController.Instance != null && InventoryUIController.Instance.IsInventoryOpen()))
        {
            return;
        }
        
        // Prioridade 3: Se nenhuma UI de interação estiver aberta, o clique é para interagir com o mundo.
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(mousePosition), Vector2.zero);

        if (hit.collider != null)
        {
            InteractableBase interactable = hit.collider.GetComponent<InteractableBase>();
            if (interactable != null)
            {
                ShowContextMenu(interactable);
            }
        }
    }
    
    private void HandleDialogueClick()
    {
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = fullDialogueText;
            isTyping = false;
        }
        else
        {
            HideDialogueBox();
        }
    }

    public void ShowContextMenu(InteractableBase interactable)
    {
        currentInteractable = interactable;
        
        Bounds objectBounds = interactable.GetComponent<Collider2D>().bounds;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(new Vector3(objectBounds.max.x, objectBounds.max.y, objectBounds.center.z));
        
        screenPos += new Vector3(10f, 10f, 0);

        contextMenu.transform.position = screenPos;
        contextMenu.SetActive(true);
    }

    public void ShowDialogue(string text)
    {
        if(dialogueBox.activeSelf) return;

        HideContextMenu();
        fullDialogueText = text;
        dialogueBox.SetActive(true);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(text));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    public void HideContextMenu() => contextMenu.SetActive(false);
    
    public void HideDialogueBox() => dialogueBox.SetActive(false);

    public void HideAllInteractionUI()
    {
        HideContextMenu();
        HideDialogueBox();
    }

    private void OnInspectClicked()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnInspect();
        }
        HideContextMenu();
    }

    private void OnUseItemClicked()
    {
        if (currentInteractable != null)
        {
            InventoryUIController.Instance.OpenInventoryForUse(currentInteractable);
        }
        HideContextMenu();
    }
}

