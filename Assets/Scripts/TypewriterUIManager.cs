using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // Necessário para a corrotina
using UnityEngine.InputSystem;
using System;

public class TypewriterUIManager : MonoBehaviour
{
    public static TypewriterUIManager Instance;

    [Header("Referências da UI")]
    [SerializeField] private GameObject typewriterPanel;
    [SerializeField] private TMP_Text playerInputDisplay; // Onde o jogador vê o que digitou
    [SerializeField] private TMP_Text responseDisplay;   // Onde o poema/Tim responde
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button enterButton;
    [SerializeField] private Button closeButton;
    // Opcional: Arraste os 26 botões de letra aqui para desativá-los
    [SerializeField] private Button[] letterButtons; 

    [Header("Configuração do Puzzle")]
    [SerializeField] private string solution = "CRAVEN";
    [SerializeField] private int maxChars = 20;
    [SerializeField] private float typingSpeed = 0.05f; // Velocidade da resposta de Tim
    
    [Header("Áudio")]
    [SerializeField] private string keySfxName = "type_key";
    [SerializeField] private string typingLoopSfxName = "typing_loop";
    [Tooltip("Duração em segundos para tocar um slice do SFX de tecla. 0 = toca o clip inteiro.")]
    [SerializeField] private float keySfxDuration = 0f;

    private AudioSource loopSource;
    private AudioSource keySource;

    private string currentText = "";
    private string poemToDisplay;
    private TypewriterController currentController;
    private bool inputLocked = false; // Trava o input enquanto Tim responde

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        typewriterPanel.SetActive(false);
        backspaceButton.onClick.AddListener(OnBackspaceClicked);
        enterButton.onClick.AddListener(OnEnterClicked);
        closeButton.onClick.AddListener(ClosePuzzle);

        // Se houver botões de letra configurados no inspector, adiciona listeners automáticos
        if (letterButtons != null)
        {
            foreach (var btn in letterButtons)
            {
                if (btn == null) continue;
                // remove para evitar duplicatas
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnLetterButtonClicked(btn));
            }
        }

        // Prepara AudioSource para o loop de digitação
        loopSource = gameObject.GetComponent<AudioSource>();
        if (loopSource == null) loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.spatialBlend = 0f;
        loopSource.loop = true;

        // Fonte dedicada para tocar SFX de tecla (evita usar sfxSource global)
        keySource = gameObject.AddComponent<AudioSource>();
        keySource.spatialBlend = 0f;
        keySource.loop = false;
    }
    
    private void Update()
    {
        // Se a UI não estiver aberta ou o input estiver travado, não faz nada
        if (!typewriterPanel.activeSelf || inputLocked) return;

        var kb = Keyboard.current;
        if (kb == null)
        {
            // Sem teclado do novo Input System disponível
            return;
        }

        // Backspace
        if (kb.backspaceKey.wasPressedThisFrame)
            OnBackspaceClicked();

        // Enter (return ou keypad enter)
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            OnEnterClicked();

        // Letras A-Z
        if (kb.anyKey.wasPressedThisFrame)
        {
            // Checa cada tecla alfabética
            if (kb.aKey.wasPressedThisFrame) OnKeyClicked("A");
            if (kb.bKey.wasPressedThisFrame) OnKeyClicked("B");
            if (kb.cKey.wasPressedThisFrame) OnKeyClicked("C");
            if (kb.dKey.wasPressedThisFrame) OnKeyClicked("D");
            if (kb.eKey.wasPressedThisFrame) OnKeyClicked("E");
            if (kb.fKey.wasPressedThisFrame) OnKeyClicked("F");
            if (kb.gKey.wasPressedThisFrame) OnKeyClicked("G");
            if (kb.hKey.wasPressedThisFrame) OnKeyClicked("H");
            if (kb.iKey.wasPressedThisFrame) OnKeyClicked("I");
            if (kb.jKey.wasPressedThisFrame) OnKeyClicked("J");
            if (kb.kKey.wasPressedThisFrame) OnKeyClicked("K");
            if (kb.lKey.wasPressedThisFrame) OnKeyClicked("L");
            if (kb.mKey.wasPressedThisFrame) OnKeyClicked("M");
            if (kb.nKey.wasPressedThisFrame) OnKeyClicked("N");
            if (kb.oKey.wasPressedThisFrame) OnKeyClicked("O");
            if (kb.pKey.wasPressedThisFrame) OnKeyClicked("P");
            if (kb.qKey.wasPressedThisFrame) OnKeyClicked("Q");
            if (kb.rKey.wasPressedThisFrame) OnKeyClicked("R");
            if (kb.sKey.wasPressedThisFrame) OnKeyClicked("S");
            if (kb.tKey.wasPressedThisFrame) OnKeyClicked("T");
            if (kb.uKey.wasPressedThisFrame) OnKeyClicked("U");
            if (kb.vKey.wasPressedThisFrame) OnKeyClicked("V");
            if (kb.wKey.wasPressedThisFrame) OnKeyClicked("W");
            if (kb.xKey.wasPressedThisFrame) OnKeyClicked("X");
            if (kb.yKey.wasPressedThisFrame) OnKeyClicked("Y");
            if (kb.zKey.wasPressedThisFrame) OnKeyClicked("Z");
        }
    }

    public void OpenPuzzle(TypewriterController controller, string poem)
    {
        currentController = controller;
        poemToDisplay = poem;
        // Se o puzzle já estiver resolvido, mostra diretamente o poema e bloqueia entrada
        bool alreadySolved = controller != null && controller.IsSolved;

        if (!alreadySolved)
        {
            currentText = "";
            inputLocked = false;
            // Reseta a UI para o estado inicial
            UpdateDisplay();
            responseDisplay.text = ""; // Limpa a resposta anterior
            responseDisplay.gameObject.SetActive(false); // Esconde o campo de resposta
            // Reativa os botões
            SetInputButtonsInteractable(true);
        }
        else
        {
            // Se já resolvido, mostra o poema imediatamente (sem precisar digitar)
            inputLocked = true;
            SetInputButtonsInteractable(false);
            responseDisplay.gameObject.SetActive(true);
            responseDisplay.text = poemToDisplay ?? "";
        }

        typewriterPanel.SetActive(true);
        PlayerMovement.Instance.LockMovement();
        // Focus first close/escape button for keyboard/controller
        if (UnityEngine.EventSystems.EventSystem.current != null && closeButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
        if (typewriterPanel != null && typewriterPanel.GetComponent<UIAutoCloseOnCancel>() == null)
        {
            var helper = typewriterPanel.AddComponent<UIAutoCloseOnCancel>();
            helper.panel = typewriterPanel;
            helper.closeButton = closeButton;
        }
    }

    public void ClosePuzzle()
    {
        typewriterPanel.SetActive(false);
        currentController = null;
        PlayerMovement.Instance.UnlockMovement();
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    // --- Funções Públicas para os Botões ---

    public void OnKeyClicked(string letter)
    {
        if (inputLocked || currentText.Length >= maxChars) return;
        currentText += letter;
        UpdateDisplay();
        PlayKeySfx();
    }

    private void OnLetterButtonClicked(Button btn)
    {
        if (btn == null) return;
        // Tenta ler o texto do filho TMP/Text
        string letter = null;
        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) letter = tmp.text;
        else
        {
            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null) letter = txt.text;
        }

        if (string.IsNullOrEmpty(letter))
        {
            // fallback para o nome do botão
            letter = btn.gameObject.name;
        }

        // Normaliza: pega apenas o primeiro caractere e transforma em maiúscula
        if (!string.IsNullOrEmpty(letter))
        {
            OnKeyClicked(letter.Substring(0, 1).ToUpper());
        }
    }

    public void OnBackspaceClicked()
    {
        if (inputLocked || currentText.Length == 0) return;
        currentText = currentText.Substring(0, currentText.Length - 1);
        UpdateDisplay();
        PlayKeySfx();
    }

    public void OnEnterClicked()
    {
        if (inputLocked) return;

        if (string.Equals(currentText, solution, StringComparison.OrdinalIgnoreCase))
        {
            // Sucesso!
            inputLocked = true; // Trava o input do jogador
            SetInputButtonsInteractable(false); // Desativa botões de input
            StartCoroutine(ShowResponse()); // Inicia a resposta de Tim
        }
        else
        {
            // Falha
            currentText = "";
            UpdateDisplay();
            // Opcional: Tocar som de falha
        }
    }

    // --- Lógica Interna ---

    private IEnumerator ShowResponse()
    {
        responseDisplay.gameObject.SetActive(true); // Mostra o campo de resposta
        
        // Lógica de "máquina de escrever" para o poema
        responseDisplay.text = "";

        // Inicia loop de digitação se houver AudioManager e nome configurado
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(typingLoopSfxName))
        {
            AudioManager.Instance.PlayLoopOnSource(loopSource, typingLoopSfxName, AudioManager.Category.UI);
        }

        string s = poemToDisplay ?? "";
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '<')
            {
                // Trata tag de rich text como unidade (ex: <b>, </b>, <i>, etc.)
                int j = s.IndexOf('>', i);
                if (j != -1)
                {
                    // Anexa toda a tag de uma vez, sem tocar SFX por caractere
                    responseDisplay.text += s.Substring(i, j - i + 1);
                    i = j + 1;
                    continue;
                }
                // Se não encontrar '>', cai fora e trata como caractere normal
            }

            // Caractere normal: adiciona e toca SFX de tecla
            responseDisplay.text += s[i];
            PlayKeySfx();
            i++;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        // Para o loop de digitação
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopOnSource(loopSource);
        }

        // Marca o puzzle como resolvido no GameManager (se houver um controller válido)
        if (currentController != null)
        {
            currentController.OnPuzzleSolved();
        }
        else
        {
            // TypewriterUIManager: currentController é null ao finalizar ShowResponse(). (log removed)
        }
        // O jogador agora pode ler a resposta e clicar em "Close"
    }

    private void SetInputButtonsInteractable(bool state)
    {
        backspaceButton.interactable = state;
        enterButton.interactable = state;
        foreach (Button button in letterButtons)
        {
            button.interactable = state;
        }
    }

    private void UpdateDisplay()
    {
        playerInputDisplay.text = currentText + "_"; // Adiciona um "cursor"
        // Opcional: Tocar som de tecla
    }

    private void PlayKeySfx()
    {
        if (AudioManager.Instance == null) return;
        if (string.IsNullOrEmpty(keySfxName)) return;
        if (keySource == null)
        {
            // fallback para o AudioManager global
            if (keySfxDuration > 0f)
                AudioManager.Instance.PlaySFXSlice(keySfxName, 0f, keySfxDuration, 1f, AudioManager.Category.UI);
            else
                AudioManager.Instance.PlaySFX(keySfxName, AudioManager.Category.UI);
            return;
        }

        // Play the full clip on the local keySource and stop it after keySfxDuration seconds
        var full = AudioManager.Instance.soundBank?.GetClip(keySfxName);
        if (full == null)
        {
            // fallback to global play if clip missing
            AudioManager.Instance.PlaySFX(keySfxName, AudioManager.Category.UI);
            return;
        }

        float scaleFull = AudioManager.Instance.masterVolume * AudioManager.Instance.sfxVolume * AudioManager.Instance.GetCategoryVolume(AudioManager.Category.UI);
        keySource.clip = full;
        keySource.loop = false;
        keySource.volume = scaleFull;
        keySource.Play();

        // If a specific short duration is requested, stop the local source after that duration
        if (keySfxDuration > 0f)
        {
            if (stopKeyCoroutine != null) StopCoroutine(stopKeyCoroutine);
            stopKeyCoroutine = StartCoroutine(StopKeyAfter(keySfxDuration));
        }
    }

    private Coroutine stopKeyCoroutine;

    private IEnumerator StopKeyAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (keySource != null && keySource.isPlaying)
        {
            keySource.Stop();
        }
        stopKeyCoroutine = null;
    }
}