using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI manager para o puzzle de quadros (números). Reutiliza uma grid de `FrameSlot`.
/// Ao resolver, revela uma `Image` (dica) que está dentro desta UI e notifica o controller.
/// </summary>
public class FramesUIManager : MonoBehaviour
{
    public static FramesUIManager Instance;

    [Header("Referências da UI")]
    [SerializeField] private GameObject puzzlePanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private FrameSlot slotPrefab;

    [Header("Audio de conclusão")]
    [SerializeField] private string completionSfxName;
    [SerializeField] private float completionSfxStart = 0f;
    [SerializeField] private float completionSfxDuration = 0f;
    [SerializeField] private AudioManager.Category completionSfxCategory = AudioManager.Category.SFX;
    [Range(0f,1f)] [SerializeField] private float completionSfxVolume = 1f;

    [Header("Reveal (dica)")]
    [Tooltip("Image dentro do painel que será ativada ao resolver")] 
    [SerializeField] private Image revealImage;
    [Header("Label Words")]
    [Tooltip("Textos mostrados em vez dos números. Index corresponde ao número 1..N.")]
    [SerializeField] private string[] labelWords = new string[] { "OVOS", "LAGARTA", "PULPA", "MARIPOSA", "MORTE" };
    [Header("Reveal Effect")]
    [Tooltip("Ativa efeito macabro de revelação (wipe + flicker)")]
    [SerializeField] private bool useMacabreReveal = true;
    [Tooltip("Duração (s) total do reveal")]
    [SerializeField] private float revealDuration = 1.2f;
    [Tooltip("Força do jitter aplicado durante o reveal (0..1)")]
    [SerializeField] [Range(0f,0.5f)] private float revealJitter = 0.06f;
    [Tooltip("Número de pequenos flickers durante o reveal")] 
    [SerializeField] private int flickerCount = 3;
    [Tooltip("Tempo máximo de pausa aleatória entre pequenos passos (s)")]
    [SerializeField] private float maxStepPause = 0.08f;
    [Header("Reveal SFX")]
    [SerializeField] private string revealSfxName;
    [SerializeField] private float revealSfxStart = 0f;
    [SerializeField] private float revealSfxDuration = 0f;
    [SerializeField] private AudioManager.Category revealSfxCategory = AudioManager.Category.SFX;
    [Range(0f,1f)] [SerializeField] private float revealSfxVolume = 1f;

    private System.Collections.IEnumerator revealCoroutine;

    private List<FrameSlot> slots = new List<FrameSlot>();
    private Sprite[] initialSprites;
    private int selectedSlot = -1;
    private FramesController currentController;
    private int[] controllerInitialNumbers = null;
    private int[] controllerCorrectNumbers = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePuzzle);

        // populate slots list from children (or leave empty if instantiating)
        foreach (Transform child in slotsContainer)
        {
            var s = child.GetComponent<FrameSlot>();
            if (s != null) slots.Add(s);
        }

        // cache initial sprites for each slot so we can reset image order reliably
        initialSprites = new Sprite[slots.Count];
        for (int i = 0; i < slots.Count; i++)
        {
            initialSprites[i] = (slots[i].frameImage != null) ? slots[i].frameImage.sprite : null;
        }

        if (revealImage != null) revealImage.gameObject.SetActive(false);
    }

    public void OpenPuzzle(FramesController controller)
    {
        currentController = controller;
        if (puzzlePanel == null) return;

        puzzlePanel.SetActive(true);
        PlayerMovement.Instance?.LockMovement();
        UIInputBlocker.Block("Frames");
        GamePauseManager.Pause("Frames");

        // prepare slots
        controllerInitialNumbers = controller != null ? controller.GetInitialSlotNumbers() : null;
        controllerCorrectNumbers = controller != null ? controller.GetCorrectOrderNumbers() : null;

        // If the puzzle is already solved, show the correct order (numbers + corresponding images)
        if (controller != null && controller.IsSolved())
        {
            // Build a mapping from number -> original sprite based on the controller's initial numbers
            var numberToSprite = new System.Collections.Generic.Dictionary<int, Sprite>();
            if (controllerInitialNumbers != null)
            {
                for (int j = 0; j < controllerInitialNumbers.Length && j < initialSprites.Length; j++)
                {
                    int num = controllerInitialNumbers[j];
                    if (!numberToSprite.ContainsKey(num))
                        numberToSprite[num] = initialSprites[j];
                }
            }

            for (int i = 0; i < slots.Count; i++)
            {
                int num = (controllerCorrectNumbers != null && i < controllerCorrectNumbers.Length) ? controllerCorrectNumbers[i] : 0;
                Sprite sprite = null;
                if (numberToSprite.TryGetValue(num, out var sp)) sprite = sp;
                slots[i].Setup(i, num, sprite, GetLabelForNumber(num));
                slots[i].SetInteractable(false);
            }
        }
        else
        {
            for (int i = 0; i < slots.Count; i++)
            {
                int num = (controllerInitialNumbers != null && i < controllerInitialNumbers.Length) ? controllerInitialNumbers[i] : 0;
                Sprite sprite = (initialSprites != null && i < initialSprites.Length) ? initialSprites[i] : (slots[i].frameImage != null ? slots[i].frameImage.sprite : null);
                slots[i].Setup(i, num, sprite, GetLabelForNumber(num));
            }
        }

        // If already solved, show reveal immediately
        if (controller != null && controller.IsSolved())
        {
            // disable interaction and show reveal
            foreach (var s in slots) s?.SetInteractable(false);
            if (revealImage != null) revealImage.gameObject.SetActive(true);
        }
    }

    public void ClosePuzzle()
    {
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
        currentController = null;
        selectedSlot = -1;
        controllerInitialNumbers = null;
        controllerCorrectNumbers = null;

        PlayerMovement.Instance?.UnlockMovement();
        UIInputBlocker.Unblock("Frames");
        GamePauseManager.Unpause("Frames");
    }

    public void OnSlotClicked(int index)
    {
        if (index < 0 || index >= slots.Count) return;

        if (selectedSlot == -1)
        {
            selectedSlot = index;
            slots[index].SetSelected(true);
            return;
        }

        if (selectedSlot == index)
        {
            slots[index].SetSelected(false);
            selectedSlot = -1;
            return;
        }

        // swap numbers
        int a = slots[selectedSlot].currentNumber;
        int b = slots[index].currentNumber;
        Sprite spriteA = slots[selectedSlot].frameImage != null ? slots[selectedSlot].frameImage.sprite : null;
        Sprite spriteB = slots[index].frameImage != null ? slots[index].frameImage.sprite : null;
        slots[selectedSlot].UpdateContent(b, spriteB, GetLabelForNumber(b));
        slots[index].UpdateContent(a, spriteA, GetLabelForNumber(a));

        slots[selectedSlot].SetSelected(false);
        selectedSlot = -1;

        CheckSolution();
    }

    private string GetLabelForNumber(int number)
    {
        if (labelWords != null && labelWords.Length > 0)
        {
            if (number >= 1 && number <= labelWords.Length)
                return labelWords[number - 1];
        }
        // fallback to numeric string
        return number.ToString();
    }

    private void CheckSolution()
    {
        if (controllerCorrectNumbers == null) return;
        for (int i = 0; i < slots.Count; i++)
        {
            int expected = (i < controllerCorrectNumbers.Length) ? controllerCorrectNumbers[i] : 0;
            if (slots[i].currentNumber != expected) return;
        }

        // solved
        if (!string.IsNullOrEmpty(completionSfxName) && AudioManager.Instance != null)
        {
            if (completionSfxDuration > 0f)
                AudioManager.Instance.PlaySFXSlice(completionSfxName, completionSfxStart, completionSfxDuration, completionSfxVolume, completionSfxCategory);
            else
                AudioManager.Instance.PlaySFX(completionSfxName, completionSfxCategory, completionSfxVolume);
        }

        // disable further interaction with slots
        foreach (var s in slots) s?.SetInteractable(false);

        // reveal the hint image inside the UI (with effect)
        if (revealImage != null)
        {
            if (useMacabreReveal)
            {
                if (revealCoroutine != null) StopCoroutine(revealCoroutine);
                revealCoroutine = RevealHintMacabre();
                StartCoroutine(revealCoroutine);
            }
            else
            {
                revealImage.gameObject.SetActive(true);
            }
        }

        // notify controller so it can mark state and collect journal
        currentController?.OnPuzzleSolved();

        // keep UI open so player can read the hint; they can close manually
    }

    private System.Collections.IEnumerator RevealHintMacabre()
    {
        // prepare image for reveal
        revealImage.gameObject.SetActive(true);

        // remember original settings
        var origType = revealImage.type;
        var origFill = revealImage.fillAmount;
        var origColor = revealImage.color;

        // prefer Fill method; fallback to simple activation
        revealImage.type = Image.Type.Filled;
        revealImage.fillMethod = Image.FillMethod.Vertical;
        revealImage.fillOrigin = 0; // bottom to top
        revealImage.fillClockwise = true;
        revealImage.fillAmount = 0f;

        // play reveal SFX once at start if available
        if (!string.IsNullOrEmpty(revealSfxName) && AudioManager.Instance != null)
        {
            if (revealSfxDuration > 0f)
                AudioManager.Instance.PlaySFXSlice(revealSfxName, revealSfxStart, revealSfxDuration, revealSfxVolume, revealSfxCategory);
            else
                AudioManager.Instance.PlaySFX(revealSfxName, revealSfxCategory, revealSfxVolume);
        }

        float elapsed = 0f;
        int flickersDone = 0;
        while (elapsed < revealDuration)
        {
            float t = elapsed / revealDuration;
            // base progress
            float baseProgress = Mathf.SmoothStep(0f, 1f, t);
            // jitter gives the impression of uneven writing strokes
            float jitter = (Mathf.PerlinNoise(Time.unscaledTime * 5f, t * 10f) - 0.5f) * revealJitter;
            float progress = Mathf.Clamp01(baseProgress + jitter);
            revealImage.fillAmount = progress;

            // occasional small pauses to simulate stroke timing
            if (Random.value < 0.02f)
            {
                float pause = Random.Range(0f, maxStepPause);
                yield return new WaitForSecondsRealtime(pause);
            }

            // intermittent flicker: briefly lower alpha then restore
            if (flickerCount > 0 && flickersDone < flickerCount && Random.value < 0.01f)
            {
                flickersDone++;
                float saved = revealImage.color.a;
                revealImage.color = new Color(origColor.r * 0.6f, origColor.g * 0.6f, origColor.b * 0.6f, 0.35f);
                yield return new WaitForSecondsRealtime(0.06f);
                revealImage.color = origColor;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // ensure fully visible
        revealImage.fillAmount = 1f;
        revealImage.type = origType;
        revealImage.color = origColor;

        // small settle/final flicker
        yield return new WaitForSecondsRealtime(0.12f);
    }
}
