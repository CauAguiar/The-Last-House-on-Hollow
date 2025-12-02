using UnityEngine;

/// <summary>
/// Interactable for Tim Craven in the basement.
/// Behavior:
/// - First inspect: plays an array of introductory dialogues (sequence).
/// - Subsequent inspects: plays a single follow-up dialogue.
/// - Using the correct item (knife) when not yet "used":
///     - grants reward item, disables animator, swaps sprite to "usedSprite"
///     - disables or mutes Tim audio
///     - marks state saved via GameStateManager
/// - Audio settings (name/volume/duration) are exposed similar to other controllers.
/// </summary>
public class TimCravenController : InteractableBase
{
    [Header("Configuração - itens")]
    [SerializeField] private InventoryItem requiredItem; // ex: knife
    [SerializeField] private InventoryItem rewardItem;    // item given when successful

    [Header("Diálogos")]
    // First-inspect dialogues embedded in script (not editable in Inspector)
    private static readonly string[] defaultFirstInspectDialogues = new string[]
    {
        "<b>Tim:</b> <color=yellow>Você... Veio...</color>",
        "<b>Charlie:</b> Bem, eu não tive muita escolha né? Tô preso aqui.",
        "<b>Charlie:</b> Você é Tim Craven, certo?",
        "<b>Tim:</b> <color=yellow>Eu era... agora eu não sei mais o que sou.</color>",
        "<b>Tim:</b> <color=yellow>Um monstro putrefato, sob o jugo da troca equivalente.</color>",
        "<b>Tim:</b> <color=yellow>Eu fui tolo, garoto. Quis mudar as leis fundamentais do universo, a única certeza da vida: a morte.</color>",
        "<b>Tim:</b> <color=yellow>E essa foi a consequência. Me desculpe por te prender assim, mas as minhas meninas merecem o paraíso.</color>",
        "<b>Tim:</b> <color=yellow>E eu devo pagar pelo meu erro.</color>",
        "<b>Tim:</b> <color=yellow>Recolha o meu coração, garoto. Finalize o ritual, e viva uma vida longa e feliz ao lado daqueles que ama.</color>",
        "<b>Tim:</b> <color=yellow>Devia ter amado mais, e até errado mais, ter visto o sol se pôr...</color>"
    };
    [Tooltip("Linha a ser exibida em inspeções posteriores (após primeira inspeção, se não tiver usado)")]
    [SerializeField] private string repeatInspectDialogue;
    [Tooltip("Linha a ser exibida após Tim ter sido " + "usado" + ".")]
    [SerializeField] private string afterUsedInspectDialogue;

    [Header("Visual / Animations")]
    [Tooltip("Sprite a aplicar quando Tim for 'usado' (após aplicar o item)")]
    [SerializeField] private Sprite usedSprite;
    [Tooltip("Animator para controlar animação de Tim (será desabilitado após o uso)")]
    [SerializeField] private Animator animatorToDisable;
    [Tooltip("SpriteRenderer do Tim, caso o SpriteRenderer não esteja no mesmo GameObject (fallback)")]
    [SerializeField] private SpriteRenderer spriteRendererOverride;

    [Header("Persistência")]
    [SerializeField] private string uniqueId;

    [Header("Audio")]
    [Tooltip("SFX name to play as Tim ambient/voice while active")]
    [SerializeField] private string timAudioName = "";
    [Tooltip("Volume for Tim audio")]
    [SerializeField] [Range(0f,1f)] private float timAudioVolume = 1f;
    [SerializeField] private AudioManager.Category timAudioCategory = AudioManager.Category.SFX;
    [Header("Audio - Uso")]
    [Tooltip("SFX a tocar quando o jogador usar o item correto em Tim")]
    [SerializeField] private string useSfxName = "";
    [Tooltip("Start offset (s) para tocar uma fatia do SFX; se <= 0 toca desde o início")]
    [SerializeField] private float useSfxStart = 0f;
    [Tooltip("Duração (s) da fatia a tocar; se <= 0 toca o SFX inteiro")]
    [SerializeField] private float useSfxDuration = 0f;
    [SerializeField] [Range(0f,1f)] private float useSfxVolume = 1f;
    [SerializeField] private AudioManager.Category useSfxCategory = AudioManager.Category.SFX;

    private bool hasBeenInspected = false;
    private bool isUsed = false;

    private SpriteRenderer sr;

    protected override void Awake()
    {
        base.Awake();
    }

    private AudioSource loopSource;
    private bool loopRegisteredWithAudioManager = false;
    private bool loopStarted = false;

    private void Start()
    {
        if (string.IsNullOrEmpty(uniqueId)) uniqueId = System.Guid.NewGuid().ToString();
        // Check persistent state
        isUsed = GameStateManager.Instance != null && GameStateManager.Instance.IsCollected(uniqueId);

        // get sprite renderer
        sr = GetComponent<SpriteRenderer>() ?? spriteRendererOverride;

        // if already used, apply visuals
        if (isUsed)
        {
            ApplyUsedVisuals();
            // if we had an ambient loop it should not start; ensure any existing loop is stopped
            loopSource = GetComponent<AudioSource>();
            if (loopSource != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopLoopOnSource(loopSource);
            }
        }
        else
        {
            // otherwise play ambient Tim audio if configured. Use a local AudioSource and register a loop
            if (!string.IsNullOrEmpty(timAudioName))
            {
                loopSource = GetComponent<AudioSource>();
                if (loopSource == null)
                {
                    loopSource = gameObject.AddComponent<AudioSource>();
                    loopSource.playOnAwake = false;
                }
                loopSource.loop = true;
                loopSource.spatialBlend = 0f;

                if (AudioManager.Instance != null && AudioManager.Instance.soundBank != null)
                {
                    // register loop with AudioManager so category volumes apply (we don't use slices; loop whole clip)
                    AudioManager.Instance.PlayLoopOnSource(loopSource, timAudioName, timAudioCategory, Mathf.Clamp01(timAudioVolume), false);
                    loopRegisteredWithAudioManager = true;
                    loopStarted = true;
                }
                else
                {
                    // Fallback: try to get clip from soundBank if present and play locally
                    AudioClip clip = AudioManager.Instance?.soundBank?.GetClip(timAudioName);
                    if (clip != null)
                    {
                        loopSource.clip = clip;
                        loopSource.volume = Mathf.Clamp01(timAudioVolume);
                        if (!loopSource.isPlaying)
                        {
                            loopSource.Play();
                            loopStarted = true;
                        }
                    }
                }
            }
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid() { uniqueId = System.Guid.NewGuid().ToString(); }

    public override void OnInspect()
    {
        if (isUsed)
        {
            if (!string.IsNullOrEmpty(afterUsedInspectDialogue) && InteractionManager.Instance != null)
                InteractionManager.Instance.ShowDialogue(afterUsedInspectDialogue);
            else
                base.OnInspect();
            return;
        }

        if (!hasBeenInspected)
        {
            hasBeenInspected = true;
            if (InteractionManager.Instance != null)
            {
                StartCoroutine(PlayFirstInspectSequence());
            }
            else
            {
                base.OnInspect();
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(repeatInspectDialogue) && InteractionManager.Instance != null)
                InteractionManager.Instance.ShowDialogue(repeatInspectDialogue);
            else
                base.OnInspect();
        }
    }

    private System.Collections.IEnumerator PlayFirstInspectSequence()
    {
        var im = InteractionManager.Instance;
        if (im == null) yield break;

        foreach (var line in defaultFirstInspectDialogues)
        {
            if (string.IsNullOrEmpty(line)) continue;

            // Show the line (InteractionManager will open the dialogue box)
            im.ShowDialogue(line);

            // Wait until the dialogue box is visible (typing started)
            yield return new UnityEngine.WaitUntil(() => im.IsDialogueOpen());

            // Wait until the player clicks to close the dialogue box (dialogue becomes inactive)
            yield return new UnityEngine.WaitUntil(() => !im.IsDialogueOpen());
        }
    }

    public override void OnUseItem(InventoryItem item)
    {
        if (isUsed)
        {
            base.OnUseItem(item);
            return;
        }

        if (item == null)
        {
            base.OnUseItem(item);
            return;
        }

        if (item == requiredItem)
        {
            // success
            isUsed = true;
            if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null)
                GameStateManager.Instance.MarkAsCollected(uniqueId);

            // give reward
            if (rewardItem != null && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(rewardItem);
            }

            // Play use SFX (slice or full) if configured
            if (!string.IsNullOrEmpty(useSfxName) && AudioManager.Instance != null)
            {
                if (useSfxDuration > 0f)
                    AudioManager.Instance.PlaySFXSlice(useSfxName, useSfxStart, useSfxDuration, useSfxVolume, useSfxCategory);
                else
                    AudioManager.Instance.PlaySFX(useSfxName, useSfxCategory, useSfxVolume);
            }

            // stop or mute audio: if we registered a loop with AudioManager, stop it; otherwise stop local source
            if (loopStarted && loopSource != null)
            {
                if (loopRegisteredWithAudioManager && AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopLoopOnSource(loopSource);
                }
                else
                {
                    loopSource.Stop();
                }
                loopStarted = false;
                loopRegisteredWithAudioManager = false;
            }

            // disable animator
            if (animatorToDisable != null)
            {
                animatorToDisable.enabled = false;
            }

            // swap sprite
            if (sr != null && usedSprite != null)
            {
                sr.sprite = usedSprite;
            }

            // show post-use dialogue if any
            if (!string.IsNullOrEmpty(afterUsedInspectDialogue) && InteractionManager.Instance != null)
            {
                InteractionManager.Instance.ShowDialogue(afterUsedInspectDialogue);
            }

            // remove required item from inventory (if desired behavior)
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveItem(requiredItem);
            }
        }
        else
        {
            base.OnUseItem(item);
        }
    }

    private void ApplyUsedVisuals()
    {
        if (animatorToDisable != null) animatorToDisable.enabled = false;
        if (sr != null && usedSprite != null) sr.sprite = usedSprite;
    }
}
