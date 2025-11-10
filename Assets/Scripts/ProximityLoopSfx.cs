using UnityEngine;

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

    private void Start()
    {
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
        if (fallbackClip != null)
        {
            source.clip = fallbackClip;
            ConfigureSource();
            source.loop = true;
            source.playOnAwake = false;
            source.Play();
        }
    }

    private void Update()
    {
        if (player == null || source.clip == null) return;

        if (updateInterval > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = updateInterval;
        }

        // Aplica volume conforme modo
        if (attenuationMode == Mode.Engine3D)
        {
            // Apenas aplica volumes globais; a atenuação por distância é feita pelo AudioSource
            float vol = baseVolume;
            if (AudioManager.Instance != null)
                vol *= AudioManager.Instance.masterVolume * AudioManager.Instance.sfxVolume;
            source.volume = vol;
        }
        else // Manual2D
        {
            float dist;
            if (planar2D)
            {
                Vector2 p = new Vector2(player.position.x, player.position.y);
                Vector2 s = new Vector2(transform.position.x, transform.position.y);
                dist = Vector2.Distance(p, s);
            }
            else
            {
                dist = Vector3.Distance(player.position, transform.position);
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
            float vol = baseVolume * shaped;
            if (AudioManager.Instance != null)
                vol *= AudioManager.Instance.masterVolume * AudioManager.Instance.sfxVolume;
            source.volume = vol;
        }
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
}
