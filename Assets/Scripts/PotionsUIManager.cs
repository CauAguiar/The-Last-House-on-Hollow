using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PotionsUIManager : MonoBehaviour
{
    public static PotionsUIManager Instance;

    [Header("Referências da UI")]
    [SerializeField] private GameObject puzzlePanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform slotsContainer; // O objeto pai onde os slots ficam
    [SerializeField] private PotionSlot slotPrefab;    // O prefab do slot (se for instanciar) ou use a lista abaixo

    [Header("Configuração Visual")]
    // Associe cada cor ao seu Sprite correspondente no Inspector
    [SerializeField] private List<PotionSpriteData> potionSprites;

    [System.Serializable]
    public struct PotionSpriteData {
        public PotionColor color;
        public Sprite sprite;
    }

    [Header("Solução")]
    // A ordem correta: Azul (Água), Vermelho (Sangue), Roxo (Magia), Verde (Veneno), Amarelo (Âmbar)
    [SerializeField] private List<PotionColor> correctOrder;

    // Estado atual
    private List<PotionSlot> slots = new List<PotionSlot>();
    private int selectedSlotIndex = -1; // -1 significa "ninguém selecionado"
    private PotionsController currentController;
    [Header("Áudio")]
    [Tooltip("Som tocado quando o puzzle for completado (nome no SoundBank)")]
    [SerializeField] private string completionSfxName;
    [Tooltip("Start time (s) of completion SFX slice")]
    [SerializeField] private float completionSfxStart = 0f;
    [Tooltip("Duration (s) of completion SFX slice; if 0, plays full clip")]
    [SerializeField] private float completionSfxDuration = 1.5f;
    [SerializeField] private AudioManager.Category completionSfxCategory = AudioManager.Category.SFX;
    [Range(0f,1f)] [SerializeField] private float completionSfxVolume = 1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        puzzlePanel.SetActive(false);
        closeButton.onClick.AddListener(ClosePuzzle);
        
        // Inicializa a lista de slots pegando os filhos do container
        int idx = 0;
        foreach(Transform child in slotsContainer)
        {
            PotionSlot slot = child.GetComponent<PotionSlot>();
            if(slot != null)
            {
                // Tenta inferir a cor a partir do sprite já definido no editor
                Sprite sprite = slot.potionImage != null ? slot.potionImage.sprite : null;
                PotionColor inferred = PotionColor.Blue; // default
                bool found = false;
                if (sprite != null && potionSprites != null)
                {
                    foreach (var data in potionSprites)
                    {
                        if (data.sprite == sprite)
                        {
                            inferred = data.color;
                            found = true;
                            break;
                        }
                    }
                }
                if (!found)
                {
                    if (potionSprites != null && potionSprites.Count > 0)
                        inferred = potionSprites[0].color;
                }

                slot.Setup(idx, inferred, sprite);
                slots.Add(slot);
                idx++;
            }
        }
    }

    public void OpenPuzzle(PotionsController controller)
    {
        currentController = controller;
        if (puzzlePanel == null)
        {
            // PotionsUIManager: puzzlePanel está nulo. (log removed)
            return;
        }

        // Traz o painel para frente
        var rt = puzzlePanel.GetComponent<RectTransform>();
        if (rt != null && rt.parent != null) rt.SetAsLastSibling();

        puzzlePanel.SetActive(true);
        PlayerMovement.Instance?.LockMovement();
        UIInputBlocker.Block("Potions");
        // Pausa o jogo para que efeitos de tempo sejam pausados (opcional)
        GamePauseManager.Pause("Potions");

        // Set UI selection to close button for keyboard/controller users
        if (UnityEngine.EventSystems.EventSystem.current != null && closeButton != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        // Auto-attach ESC close helper when available
        if (puzzlePanel != null && puzzlePanel.GetComponent<UIAutoCloseOnCancel>() == null)
        {
            var helper = puzzlePanel.AddComponent<UIAutoCloseOnCancel>();
            helper.panel = puzzlePanel;
            helper.closeButton = closeButton;
        }

        // Reseta a seleção
        DeselectAll();
    }

    public void ClosePuzzle()
    {
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
        currentController = null;
        PlayerMovement.Instance?.UnlockMovement();
        UIInputBlocker.Unblock("Potions");
        GamePauseManager.Unpause("Potions");
    }

    // --- Lógica Principal ---

    public void OnSlotClicked(int index)
    {
        // Se ninguém estava selecionado, seleciona este
        if (selectedSlotIndex == -1)
        {
            selectedSlotIndex = index;
            slots[index].SetSelected(true);
            // Opcional: Som de clique
        }
        // Se clicou no mesmo slot, desseleciona
        else if (selectedSlotIndex == index)
        {
            DeselectAll();
        }
        // Se clicou em OUTRO slot, faz a troca!
        else
        {
            SwapSlots(selectedSlotIndex, index);
            DeselectAll();
            CheckSolution();
        }
    }

    private void SwapSlots(int indexA, int indexB)
    {
        // Guarda os dados do Slot A temporariamente
        PotionColor colorA = slots[indexA].currentColor;
        Sprite spriteA = slots[indexA].potionImage.sprite;

        // Slot A recebe dados do Slot B
        slots[indexA].UpdateContent(slots[indexB].currentColor, slots[indexB].potionImage.sprite);

        // Slot B recebe dados guardados do A
        slots[indexB].UpdateContent(colorA, spriteA);

        // Opcional: Som de vidro/poção
    }

    private void DeselectAll()
    {
        selectedSlotIndex = -1;
        foreach (var slot in slots) slot.SetSelected(false);
    }

    private void CheckSolution()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            // Se qualquer slot estiver diferente da ordem correta, falha
            if (slots[i].currentColor != correctOrder[i]) return;
        }

        // Se passou pelo loop, venceu!
        // Puzzle resolved (debug log removed to reduce console noise)

        // Toca som de conclusão se houver
        if (!string.IsNullOrEmpty(completionSfxName) && AudioManager.Instance != null)
        {
            if (completionSfxDuration > 0f)
                AudioManager.Instance.PlaySFXSlice(completionSfxName, completionSfxStart, completionSfxDuration, completionSfxVolume, completionSfxCategory);
            else
                AudioManager.Instance.PlaySFX(completionSfxName, completionSfxCategory, completionSfxVolume);
        }
        // Atualiza visual do controller (potions) e notifica solução
        if (currentController != null) currentController.OnPuzzleSolved();
        ClosePuzzle();
    }
    
    // Helper para pegar sprite (útil se você quiser randomizar no início)
    public Sprite GetSpriteForColor(PotionColor color)
    {
        foreach(var data in potionSprites)
            if(data.color == color) return data.sprite;
        return null;
    }
}