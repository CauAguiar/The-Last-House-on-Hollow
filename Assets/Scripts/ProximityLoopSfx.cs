using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reproduz um loop (ex: lareira) que fica mais alto conforme o jogador se aproxima.
/// - Exige um AudioSource configurado para loop.
/// - Ajusta volume de 0 até volumeBase * curvaDistancia.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ProximityLoopSfx : MonoBehaviour
{
    public enum Mode { Engine3D, Manual2D }

    [Header("Configuração")]
    [Tooltip("Nome do som no SoundBank (opcional). Se vazio, usa o clip do AudioSource.")]
    [SerializeField] private string soundName;
    [Tooltip("Clip manual (usado se soundName vazio).")]
    [SerializeField] private AudioClip fallbackClip;
    [Tooltip("Referência do jogador (se vazio, procura PlayerMovement.Instance).")]
    [SerializeField] private Transform player;
    [Tooltip("Distância em que o som atinge volume máximo.")]
    [SerializeField] private float maxDistance = 5f;
    [Tooltip("Distância mínima para iniciar fade-in (0 = da posição exata).")]
    [SerializeField] private float minDistance = 0.5f;
    [Tooltip("Volume base antes de aplicar categorias do AudioManager.")]
    [SerializeField] private float baseVolume = 1f;
    [Tooltip("Modo de atenuação: Engine3D usa rolloff do próprio AudioSource; Manual2D calcula volume por curva.")]
    [SerializeField] private Mode attenuationMode = Mode.Engine3D;

    [Header("Engine3D (AudioSource)")]
    [Tooltip("Rolloff do AudioSource quando usar Engine3D.")]
    [SerializeField] private AudioRolloffMode engineRolloff = AudioRolloffMode.Logarithmic;
    [Tooltip("Se verdadeiro, usa curva custom no AudioSource.")]
    [SerializeField] private bool useCustomEngineCurve = false;
    [Tooltip("Curva custom de rolloff do AudioSource (X = distância, Y = ganho).")]
    [SerializeField] private AnimationCurve engineRolloffCurve;

    [Header("Manual2D (Curva Própria)")]
    [Tooltip("Curva para moldar o volume (X = fator normalizado 0..1 da proximidade, Y = ganho). Se nula, usa linear.")]
    [SerializeField] private AnimationCurve manualVolumeCurve;
    [Tooltip("Atualiza a cada X segundos (0 = todo frame).")]
    [SerializeField] private float updateInterval = 0.1f;
    [Tooltip("Quando ligado, calcula a distância em plano 2D (XY), ignorando Z. Ideal para jogos 2D.")]
    [SerializeField] private bool planar2D = true;

    private AudioSource source;
    private float timer;
    private bool initialized;
    
    [Header("Alvo (Persistente)")]
    [Tooltip("Transform de referência do som no mundo (ex: lareira). Se vazio, tenta achar por tag ou nome quando a cena carregar.")]
    [SerializeField] private Transform target;
    [Tooltip("Tag do alvo para auto-resolver quando a cena carregar.")]
    [SerializeField] private string targetTag;
    [Tooltip("Nome do GameObject alvo para auto-resolver quando a cena carregar (usado se tag vazia).")]
    [SerializeField] private string targetName;
    [Tooltip("Se verdadeiro, só toca quando o alvo existe na cena; caso contrário, pausa/silencia ao sair.")]
    [SerializeField] private bool onlyWhenTargetPresent = true;
    [Tooltip("Se definido, o som só toca nessas cenas (use nomes exatos). Se vazio, toca em qualquer cena.")]
    [SerializeField] private string[] enabledScenes;

    [Header("Fade")]
    [Tooltip("Tempo de fade-in em segundos.")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [Tooltip("Tempo de fade-out em segundos.")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [Tooltip("Usar tempo não escalado (ignora pausas) para o fade.")]
    [SerializeField] private bool useUnscaledTime = true;
    private float currentVolume = 0f; // volume aplicado ao AudioSource após fade
    private float targetVolume = 0f;  // volume alvo calculado por distância / categoria

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (manualVolumeCurve == null || manualVolumeCurve.length == 0)
        {
            manualVolumeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
        if (engineRolloffCurve == null || engineRolloffCurve.length == 0)
        {
            engineRolloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f); // 1 perto, 0 longe
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResolvePlayerIfNeeded();
        ResolveTargetIfNeeded();
        ResolveClipIfNeeded();
        if (player == null && PlayerMovement.Instance != null)
        {
            player = PlayerMovement.Instance.transform;
        }

        // Carrega clip
        if (!string.IsNullOrEmpty(soundName) && AudioManager.Instance != null)
        {
            var clip = AudioManager.Instance.soundBank.GetClip(soundName);
            if (clip != null) fallbackClip = clip;
        }
        TryStartPlayback();
    }

    private void OnDisable()
    {
        initialized = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (!initialized)
        {
            // Tenta reinicializar se algo resetou (após troca de cena, etc.)
            ResolvePlayerIfNeeded();
            ResolveTargetIfNeeded();
            ResolveClipIfNeeded();
            if (source != null && fallbackClip != null)
            {
                ConfigureSource();
                source.loop = true;
                source.playOnAwake = false;
                source.clip = fallbackClip;
                source.Play();
                initialized = true;
            }
        }

        if (player == null || source == null || source.clip == null) return;

        // Determina se cena e alvo permitem tocar (gating)
        bool allowed = true;
        if (enabledScenes != null && enabledScenes.Length > 0)
        {
            string cur = SceneManager.GetActiveScene().name;
            allowed = false;
            for (int i = 0; i < enabledScenes.Length; i++)
            {
                if (!string.IsNullOrEmpty(enabledScenes[i]) && enabledScenes[i] == cur) { allowed = true; break; }
            }
        }
        if (allowed && onlyWhenTargetPresent && target == null)
            allowed = false;

        if (updateInterval > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = updateInterval;
        }

        // Calcula volume alvo conforme modo
        float computedVol = 0f;
        if (allowed)
        {
            if (attenuationMode == Mode.Engine3D)
            {
                computedVol = baseVolume;
                if (AudioManager.Instance != null)
                    computedVol *= AudioManager.Instance.masterVolume * AudioManager.Instance.sfxVolume * AudioManager.Instance.GetCategoryVolume(AudioManager.Category.Ambience);
                // Posiciona a fonte se alvo existe (mantém spatial correto)
                if (target != null)
                    source.transform.position = target.position;
            }
            else // Manual2D
            {
                float dist;
                if (planar2D)
                {
                    Vector2 p = new Vector2(player.position.x, player.position.y);
                    Vector2 s = new Vector2((target != null ? target.position.x : transform.position.x), (target != null ? target.position.y : transform.position.y));
                    dist = Vector2.Distance(p, s);
                }
                else
                {
                    Vector3 pos = target != null ? target.position : transform.position;
                    dist = Vector3.Distance(player.position, pos);
                }
                float t;
                if (maxDistance <= minDistance)
                {
                    t = (dist <= minDistance) ? 1f : 0f;
                }
                else if (dist <= minDistance)
                {
                    t = 1f;
                }
                else if (dist >= maxDistance)
                {
                    t = 0f;
                }
                else
                {
                    float range = maxDistance - minDistance;
                    float d = dist - minDistance;
                    t = 1f - (d / range);
                }
                t = Mathf.Clamp01(t);
                float shaped = manualVolumeCurve.Evaluate(t);
                computedVol = baseVolume * shaped;
                if (AudioManager.Instance != null)
                    computedVol *= AudioManager.Instance.masterVolume * AudioManager.Instance.sfxVolume * AudioManager.Instance.GetCategoryVolume(AudioManager.Category.Ambience);
            }
        }
        targetVolume = computedVol; // se não allowed, fica 0

        // Fade suave
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (targetVolume > currentVolume)
        {
            if (fadeInDuration > 0f)
            {
                float step = (targetVolume / fadeInDuration) * dt;
                currentVolume = Mathf.Min(currentVolume + step, targetVolume);
            }
            else currentVolume = targetVolume;
        }
        else if (targetVolume < currentVolume)
        {
            if (fadeOutDuration > 0f)
            {
                float step = (Mathf.Max(currentVolume, 0.0001f) / fadeOutDuration) * dt; // proporcional ao volume atual
                currentVolume = Mathf.Max(currentVolume - step, targetVolume);
            }
            else currentVolume = targetVolume;
        }
        source.volume = currentVolume;
    }

    private void ConfigureSource()
    {
        // Configura AudioSource conforme o modo escolhido
        if (attenuationMode == Mode.Engine3D)
        {
            source.spatialBlend = 1f;
            source.minDistance = Mathf.Max(0f, minDistance);
            source.maxDistance = Mathf.Max(source.minDistance + 0.01f, maxDistance);
            source.rolloffMode = engineRolloff;
            if (useCustomEngineCurve)
            {
                source.rolloffMode = AudioRolloffMode.Custom;
                // A curva custom do AudioSource usa X como distância (em unidades) e Y como ganho
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, engineRolloffCurve);
            }
        }
        else // Manual2D
        {
            source.spatialBlend = 0f; // 2D, volume manual
        }
    }

    private void ResolvePlayerIfNeeded()
    {
        if (player == null && PlayerMovement.Instance != null)
            player = PlayerMovement.Instance.transform;
    }

    private void ResolveTargetIfNeeded()
    {
        if (target != null) return;
        if (!string.IsNullOrEmpty(targetTag))
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go != null) { target = go.transform; return; }
        }
        if (!string.IsNullOrEmpty(targetName))
        {
            var go = GameObject.Find(targetName);
            if (go != null) { target = go.transform; }
        }
    }

    private void ResolveClipIfNeeded()
    {
        if (fallbackClip == null && !string.IsNullOrEmpty(soundName) && AudioManager.Instance != null)
        {
            var clip = AudioManager.Instance.soundBank.GetClip(soundName);
            if (clip != null) fallbackClip = clip;
        }
    }

    private void TryStartPlayback()
    {
        if (fallbackClip == null || source == null) return;
        source.clip = fallbackClip;
        ConfigureSource();
        source.loop = true;
        source.playOnAwake = false;
        currentVolume = 0f;
        targetVolume = 0f;
        source.volume = 0f; // inicia silencioso para fade-in
        source.Play();
        initialized = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-resolve quando trocamos de cena
        ResolvePlayerIfNeeded();
        ResolveTargetIfNeeded();
        ResolveClipIfNeeded();
        initialized = false; // força reconfiguração no próximo Update
    }
}
