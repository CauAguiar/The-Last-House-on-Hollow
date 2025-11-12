using UnityEngine;

/// <summary>
/// Controla a lógica da pia, que é consertada com a Chave de Rosca.
/// </summary>
public class SinkController : InteractableBase
{
    [Header("Configuração do Puzzle")]
    [Tooltip("O 'molde' do item (ScriptableObject) da Chave de Rosca.")]
    [SerializeField] private InventoryItem wrenchItem;
    
    [Tooltip("O efeito de vapor/névoa que será ativado.")]
    [SerializeField] private ParticleSystem steamVFX;

    [Tooltip("O Animator da pia para tocar a animação 'ligada'.")]
    [SerializeField] private Animator animator;
    
    [Header("Conexão com o Espelho")]
    [Tooltip("Arraste o objeto do Espelho que está na cena aqui.")]
    [SerializeField] private MirrorController mirrorToActivate;

    [Header("Controle de Estado")]
    [Tooltip("ID único para salvar o estado 'consertado' da pia.")]
    [SerializeField] private string uniqueId;

    private bool isFixed = false;

    // Nome do parâmetro bool no Animator para ligar/desligar (opcional se existir). Deixe vazio para usar Play().
    [SerializeField] private string animatorOnBool = "IsOn";

    // Nome do estado da animação quando ligada (fallback se não usar bool)
    [SerializeField] private string animatorOnStateName = "Sink_On_State";
    [SerializeField] private string animatorOffStateName = "Sink_Off_State"; // opcional

    private bool animatorSupportsBool;

    [Header("Feedback e Áudio")]
    [Tooltip("Texto exibido ao inspecionar a pia DEPOIS de consertada.")]
    [TextArea(2,4)]
    [SerializeField] private string postFixInspectionText = "A pia agora está funcionando bem.";

    [Tooltip("Fonte de áudio da pia (loop de água). Será acionada ao consertar.")]
    [SerializeField] private AudioSource sinkLoopSource;
    [Tooltip("Nome do som no SoundBank para o loop da pia (opcional, se o clip não estiver no AudioSource).")]
    [SerializeField] private string sinkLoopSoundName;

    [Header("Diagnóstico / Ajustes")]
    [Tooltip("Se marcado, imprime logs detalhados sobre o estado do áudio da pia.")]
    [SerializeField] private bool debugAudio = false;
    [Tooltip("Força um fallback manual caso o registro no AudioManager não inicie a reprodução na primeira tentativa.")]
    [SerializeField] private bool forceFallbackPlay = true;

    [Header("Slicing do Áudio da Pia")]
    [Tooltip("Habilita modo onde tocamos um trecho inicial (torneira ligando) e depois loopamos um intervalo configurável do mesmo clip.")]
    [SerializeField] private bool useSlicedLoopMode = false;
    [Tooltip("Nome do clip no SoundBank usado para o início e o loop (mesmo clip). Se vazio, usa 'sinkLoopSoundName'.")]
    [SerializeField] private string sinkClipForSlices;
    [Tooltip("Início (segundos) do SFX da torneira ligando dentro do clip.")]
    [SerializeField] private float startSfxStart = 0f;
    [Tooltip("Duração (segundos) do SFX da torneira ligando.")]
    [SerializeField] private float startSfxDuration = 0.5f;
    [Tooltip("Início (segundos) do trecho contínuo para loop dentro do clip.")]
    [SerializeField] private float loopRegionStart = 0.5f;
    [Tooltip("Duração (segundos) do trecho contínuo para loop dentro do clip.")]
    [SerializeField] private float loopRegionDuration = 2.0f;

    private Coroutine sinkAudioRoutine;

    private void Start()
    {
        // Evita considerar UniqueId vazio como já coletado
        if (string.IsNullOrEmpty(uniqueId))
        {
            // Gera automaticamente para prevenir colisões de estado
            uniqueId = System.Guid.NewGuid().ToString();
            Debug.LogWarning($"[SinkController] UniqueId estava vazio. Gerado novo: {uniqueId}");
        }

        animatorSupportsBool = animator != null && !string.IsNullOrEmpty(animatorOnBool) && HasAnimatorParameter(animatorOnBool, AnimatorControllerParameterType.Bool);

        isFixed = GameStateManager.Instance.IsCollected(uniqueId);

        if (isFixed)
        {
            ApplyFixedVisuals();
            StartSinkAudio();
        }
        else
        {
            ApplyInitialOffVisuals();
            StopSinkAudio();
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Sobrescreve a lógica de uso de item.
    /// </summary>
    public override void OnUseItem(InventoryItem item)
    {
        // Se já foi consertada, não faz mais nada.
        if (isFixed)
        {
            InteractionManager.Instance.ShowDialogue("A pia já está funcionando.");
            return;
        }

        // Verifica se o item usado é a Chave de Rosca
        if (item == wrenchItem)
        {
            if (wrenchItem == null)
            {
                InteractionManager.Instance.ShowDialogue("Algo está errado: a chave não está configurada.");
                return;
            }

            isFixed = true;
            GameStateManager.Instance.MarkAsCollected(uniqueId); // Salva o estado

            ApplyFixedVisuals();
            StartSinkAudio();

            if (mirrorToActivate != null)
            {
                mirrorToActivate.ShowClue();
            }

            InteractionManager.Instance.ShowDialogue("Consegui consertar. A água quente está saindo...");
        }
        else
        {
            // Se usou o item errado, chama a lógica padrão
            base.OnUseItem(item);
        }
    }

    public override void OnInspect()
    {
        if (isFixed && !string.IsNullOrEmpty(postFixInspectionText))
        {
            InteractionManager.Instance.ShowDialogue(postFixInspectionText);
            return;
        }
        base.OnInspect();
    }

    private void ApplyInitialOffVisuals()
    {
        if (animator == null) return;
        if (animatorSupportsBool)
        {
            animator.SetBool(animatorOnBool, false);
        }
        else if (!string.IsNullOrEmpty(animatorOffStateName))
        {
            animator.Play(animatorOffStateName, 0, 0f);
        }

        if (steamVFX != null)
        {
            steamVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void ApplyFixedVisuals()
    {
        if (animator != null)
        {
            if (animatorSupportsBool)
                animator.SetBool(animatorOnBool, true);
            else if (!string.IsNullOrEmpty(animatorOnStateName))
                animator.Play(animatorOnStateName, 0, 0f);
        }
        if (steamVFX != null && !steamVFX.isPlaying)
        {
            steamVFX.Play();
        }
    }

    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName && p.type == type) return true;
        }
        return false;
    }

    private void StartSinkAudio()
    {
        if (sinkLoopSource == null)
        {
            if (debugAudio) Debug.LogWarning("[SinkController] StartSinkAudio abortado: sinkLoopSource nulo.");
            return;
        }

        // Garante estado básico do AudioSource
        sinkLoopSource.enabled = true;
        sinkLoopSource.mute = false;
        sinkLoopSource.loop = true;
        sinkLoopSource.playOnAwake = false;

        bool usedSoundBank = false;

        if (AudioManager.Instance != null)
        {
            // Se o modo slice estiver ativo, tocamos um trecho inicial e em seguida iniciamos o loop do trecho configurado
            if (useSlicedLoopMode)
            {
                string clipName = !string.IsNullOrEmpty(sinkClipForSlices) ? sinkClipForSlices : sinkLoopSoundName;
                if (!string.IsNullOrEmpty(clipName))
                {
                    if (sinkAudioRoutine != null) StopCoroutine(sinkAudioRoutine);
                    sinkAudioRoutine = StartCoroutine(StartSinkAudioRoutine(clipName));
                    usedSoundBank = true;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(sinkLoopSoundName))
                {
                    AudioManager.Instance.PlayLoopOnSource(sinkLoopSource, sinkLoopSoundName, AudioManager.Category.Ambience, 1f, false);
                    usedSoundBank = true;
                }
                else if (sinkLoopSource.clip != null)
                {
                    // registra clip existente
                    AudioManager.Instance.PlayLoopOnSource(sinkLoopSource, sinkLoopSource.clip.name, AudioManager.Category.Ambience, 1f, true);
                }
            }
        }
        else
        {
            // Fallback sem AudioManager
            if (debugAudio) Debug.LogWarning("[SinkController] AudioManager.Instance nulo. Usando fallback manual para tocar loop.");
        }

        // Fallback manual caso clip tenha sido atribuído mas não esteja tocando
        if (forceFallbackPlay)
        {
            if (!sinkLoopSource.isPlaying && sinkLoopSource.clip != null)
            {
                sinkLoopSource.Play();
                if (debugAudio) Debug.Log("[SinkController] Fallback manual: Play() chamado.");
            }
        }

        if (debugAudio)
        {
            string clipName = sinkLoopSource.clip != null ? sinkLoopSource.clip.name : "(sem clip)";
            Debug.Log($"[SinkController] StartSinkAudio -> clip={clipName}, isPlaying={sinkLoopSource.isPlaying}, volume={sinkLoopSource.volume:F3}, usedSoundBank={usedSoundBank}");
        }
    }

    private System.Collections.IEnumerator StartSinkAudioRoutine(string clipName)
    {
        // 1) Toca SFX inicial (torneira ligando), se duração > 0
        if (startSfxDuration > 0.01f)
        {
            AudioManager.Instance.PlaySFXSlice(clipName, startSfxStart, startSfxDuration, 1f, AudioManager.Category.Ambience);
            if (debugAudio) Debug.Log($"[SinkController] SFX inicial tocado: {clipName} @ {startSfxStart:F2}s x {startSfxDuration:F2}s");
            yield return new WaitForSecondsRealtime(startSfxDuration);
        }

        // 2) Inicia loop do trecho contínuo configurado
        if (loopRegionDuration > 0.01f)
        {
            AudioManager.Instance.PlayLoopSliceOnSource(sinkLoopSource, clipName, loopRegionStart, loopRegionDuration, AudioManager.Category.Ambience, 1f, false);
            if (debugAudio) Debug.Log($"[SinkController] Loop slice iniciado: {clipName} @ {loopRegionStart:F2}s x {loopRegionDuration:F2}s");
        }
        else
        {
            // Duração inválida -> fallback para loop inteiro
            AudioManager.Instance.PlayLoopOnSource(sinkLoopSource, clipName, AudioManager.Category.Ambience, 1f, false);
            if (debugAudio) Debug.LogWarning("[SinkController] loopRegionDuration inválido. Fallback para loop completo.");
        }
    }

    private void StopSinkAudio()
    {
        if (sinkLoopSource == null) return;
        if (sinkAudioRoutine != null)
        {
            StopCoroutine(sinkAudioRoutine);
            sinkAudioRoutine = null;
        }
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopLoopOnSource(sinkLoopSource);
        else if (sinkLoopSource.isPlaying)
            sinkLoopSource.Stop();
    }
}