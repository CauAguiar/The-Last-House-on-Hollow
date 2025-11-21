using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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
        playerControls.UI.Enable();
        playerControls.Player.Interact.performed += OnInteractPerformed; 
        playerControls.UI.Cancel.performed += OnUICancelPerformed;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Player.Disable();
            playerControls.UI.Disable();
            playerControls.Player.Interact.performed -= OnInteractPerformed;
            playerControls.UI.Cancel.performed -= OnUICancelPerformed;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (contextMenu != null) contextMenu.SetActive(false);
        if (dialogueBox != null) dialogueBox.SetActive(false);

        if (inspectButton != null) inspectButton.onClick.AddListener(OnInspectClicked);
        if (useItemButton != null) useItemButton.onClick.AddListener(OnUseItemClicked);
    }
    
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Prioridade 1: Diálogo sempre consome o clique, mesmo com bloqueio global ativo
        if (dialogueBox != null && dialogueBox.activeSelf)
        {
            HandleDialogueClick();
            return; // Não prossegue para interações do mundo
        }

        // Bloqueio global: impede interação com o mundo/contexto quando QUALQUER outra UI (inventário, inspeção, etc.) estiver aberta
        if (UIInputBlocker.IsBlocked)
        {
            return; // Mas já teríamos retornado se fosse diálogo acima
        }
        
        // Prioridade 2: Se o inventário, o painel de inspeção ou o menu de contexto estiverem abertos,
        // o clique será tratado pelo sistema de UI (botões). Ignoramos a interação com o mundo.
        if (contextMenu.activeSelf || 
            (InventoryUIController.Instance != null && InventoryUIController.Instance.IsInventoryOpen()) ||
            (ItemInspectionController.Instance != null && ItemInspectionController.Instance.IsInspectionOpen()))
        {
            return;
        }
        
        // Para evitar duplicação de input, o InteractionManager NÃO processa mais cliques no mundo.
        // A responsabilidade por raycasts e seleção de interactables foi movida para
        // `PlayerInteractionController`. Aqui mantemos apenas o comportamento de diálogo
        // (avançar/fechar) para que o jogador possa usar o mesmo botão para dialogues.
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

    // Helper to align with Unity's special null for destroyed objects
    private bool IsAlive(Object obj) => obj != null;

    public void ShowContextMenu(InteractableBase interactable)
    {
        // Validate references – avoid MissingReferenceException when objects were destroyed (e.g., after scene change)
        if (!IsAlive(interactable))
        {
            Debug.LogWarning("ShowContextMenu chamado com um Interactable destruído ou nulo. Ignorando.");
            return;
        }

        if (!IsAlive(contextMenu))
        {
            Debug.LogWarning("ContextMenu não está atribuído ou foi destruído. Ignorando a abertura do menu de contexto.");
            return;
        }

        // Allow the interactable to veto showing the context menu.
        if (!interactable.CanShowContextMenu())
        {
            return;
        }

        currentInteractable = interactable;

        // Try to compute a reasonable screen position
        Vector3 screenPos;
        var col = interactable.GetComponent<Collider2D>();
        if (col != null && Camera.main != null)
        {
            var b = col.bounds;
            screenPos = Camera.main.WorldToScreenPoint(new Vector3(b.max.x, b.max.y, b.center.z));
        }
        else
        {
            // Fallback to mouse position if no collider or camera is available
            screenPos = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }

        screenPos += new Vector3(10f, 10f, 0);

        // Safely position and show the context menu
        Transform cmTransform = contextMenu != null ? contextMenu.transform : null;
        if (cmTransform != null)
        {
            cmTransform.position = screenPos;
        }
        if (contextMenu != null)
        {
            contextMenu.SetActive(true);
            UIInputBlocker.Block("ContextMenu");
        }

        // Traz o menu para frente na hierarquia (caso esteja dentro de um Canvas)
        var rect = contextMenu != null ? contextMenu.GetComponent<RectTransform>() : null;
        if (rect != null && rect.parent != null)
        {
            rect.SetAsLastSibling();
        }

        // Garante que os botões estejam interagíveis e que o EventSystem selecione o primeiro botão
    if (inspectButton != null) inspectButton.interactable = true;
    if (useItemButton != null) useItemButton.interactable = true;

        if (UnityEngine.EventSystems.EventSystem.current != null && inspectButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(inspectButton.gameObject);
        }
    }

    public void ShowDialogue(string text)
    {
        if(dialogueBox.activeSelf) return;

    HideContextMenu();
        fullDialogueText = text;
    if (dialogueBox != null) { dialogueBox.SetActive(true); UIInputBlocker.Block("Dialogue"); }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(text));
    }

    /// <summary>
    /// Coroutine aprimorada que digita o texto caractere por caractere,
    /// mas pula as tags de Rich Text para que elas não apareçam na tela.
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        string originalText = text;
        string displayedText = "";
        int i = 0;

        while (i < originalText.Length)
        {
            // Verifica se o caractere atual é o início de uma tag
            if (originalText[i] == '<')
            {
                // Encontra o final da tag
                int endIndex = originalText.IndexOf('>', i);
                if (endIndex != -1)
                {
                    // Adiciona a tag inteira de uma vez
                    displayedText += originalText.Substring(i, endIndex - i + 1);
                    i = endIndex; // Pula o ponteiro para o final da tag
                }
            }
            else
            {
                // Adiciona o caractere normal e espera
                displayedText += originalText[i];
                dialogueText.text = displayedText;
                // Usa tempo real para funcionar mesmo quando o jogo está pausado (Time.timeScale = 0)
                yield return new WaitForSecondsRealtime(typingSpeed);
            }
            i++;
        }

        // Garante que o texto final seja o texto completo com todas as tags
        dialogueText.text = originalText;
        isTyping = false;
    }

    public void HideContextMenu()
    {
        if (contextMenu != null) contextMenu.SetActive(false);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        UIInputBlocker.Unblock("ContextMenu");
    }

    public void HideDialogueBox()
    {
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }
        UIInputBlocker.Unblock("Dialogue");
    }

    public void HideAllInteractionUI()
    {
        HideContextMenu();
        HideDialogueBox();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Sempre fecha qualquer UI de interação ao mudar de cena para evitar popups "órfãos".
        HideAllInteractionUI();
        UIInputBlocker.ClearAll();
        GamePauseManager.ClearAll();

        // Garantir que exista um EventSystem ativo na nova cena para que botões sejam clicáveis.
        EnsureEventSystemExists();

        // Se houver múltiplos EventSystems (por exemplo, um persistente + um na cena), normalizamos para apenas um.
        NormalizeEventSystems(scene);
    }

    private void EnsureEventSystemExists()
    {
    if (EventSystem.current != null) return;

    // Cria um EventSystem local de cena se não existir. Não o marcamos como DontDestroyOnLoad
    // para evitar que ele persista e gere duplicatas ao trocar de cena.
    GameObject es = new GameObject("EventSystem");
    es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
    es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
    es.AddComponent<StandaloneInputModule>();
#endif
    Debug.LogWarning("EventSystem ausente na cena — um EventSystem fallback foi criado automaticamente.");
    }

    private void NormalizeEventSystems(Scene scene)
    {
    // Use the newer FindObjectsByType when available (faster and not deprecated).
#if UNITY_2023_1_OR_NEWER
    var all = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
#else
    var all = Object.FindObjectsOfType<EventSystem>();
#endif
    if (all == null || all.Length <= 1) return;

    // Preferir o EventSystem que pertence à cena carregada (se houver)
    EventSystem preferred = null;
    foreach (var es in all)
    {
        if (es.gameObject.scene == scene)
        {
        preferred = es;
        break;
        }
    }

    // Se não encontrou um na cena, apenas escolha o primeiro como preferido
    if (preferred == null) preferred = all[0];

    // Destrói todos os outros EventSystems para garantir exatamente 1
    foreach (var es in all)
    {
        if (es == preferred) continue;
        // Protege caso algum EventSystem seja parte essencial — geralmente seguro para fallbacks
        Destroy(es.gameObject);
    }
    }

    private void OnInspectClicked()
    {
        if (currentInteractable != null && IsAlive(currentInteractable))
        {
            currentInteractable.OnInspect();
        }
        HideContextMenu();
    }

    private void OnUICancelPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // Close in most-recently-open order
        if (dialogueBox != null && dialogueBox.activeSelf)
        {
            HideDialogueBox();
            return;
        }

        if (contextMenu != null && contextMenu.activeSelf)
        {
            HideContextMenu();
            return;
        }

        // Inventory
        if (InventoryUIController.Instance != null && InventoryUIController.Instance.IsInventoryOpen())
        {
            InventoryUIController.Instance.CloseInventory();
            return;
        }

        // Item inspection
        if (ItemInspectionController.Instance != null && ItemInspectionController.Instance.IsInspectionOpen())
        {
            ItemInspectionController.Instance.CloseInspection();
            return;
        }

        // Other puzzle UIs with standard ClosePuzzle naming pattern
        // Call common ClosePuzzle methods (managers should no-op if already closed)
        TypewriterUIManager.Instance?.ClosePuzzle();
        PotionsUIManager.Instance?.ClosePuzzle();
        PianoUIManager.Instance?.ClosePuzzle();
        ChestUIManager.Instance?.ClosePuzzle();
        ClockUIManager.Instance?.ClosePuzzle();
    }

    private void OnUseItemClicked()
    {
        if (currentInteractable != null && IsAlive(currentInteractable))
        {
            InventoryUIController.Instance.OpenInventoryForUse(currentInteractable);
        }
        HideContextMenu();
    }
}

