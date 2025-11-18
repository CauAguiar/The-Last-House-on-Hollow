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
                    Debug.LogWarning($"PotionSlot at index {idx} has no matching sprite in PotionsUIManager.potionSprites. Defaulting to {inferred}.");
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
            Debug.LogWarning("PotionsUIManager: puzzlePanel está nulo.");
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
        Debug.Log("Puzzle das Poções Resolvido!");
        currentController.OnPuzzleSolved();
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