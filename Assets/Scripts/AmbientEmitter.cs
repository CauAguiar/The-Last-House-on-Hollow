using UnityEngine;

/// <summary>
/// Adicione este componente a um GameObject que tem um AudioSource para efeitos de ambiente (chuva, rangido, vento, etc.).
/// Ele registra o AudioSource no AudioManager como um loop (PlayLoopOnSource) e garante que os volumes globais sejam aplicados.
/// - Para sons que devem ser ouvidos de qualquer lugar da casa, use spatialBlend = 0 (2D) no AudioSource.
/// - Para sons localizados (ex: rangido perto de uma porta), use spatialBlend > 0.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AmbientEmitter : MonoBehaviour
{
    [Tooltip("Nome do som no SoundBank a ser tocado por este emitter (se o AudioSource não tiver clip definido)")]
    public string soundName;

    [Tooltip("Categoria usada pelo AudioManager (UI, Ambience, etc.)")]
    public AudioManager.Category category = AudioManager.Category.Ambience;

    [Tooltip("Se verdadeiro, o AudioSource será tocado imediatamente como loop; caso contrário, o clip do AudioSource será usado.")]
    public bool useSoundBankClip = true;

    [Tooltip("Escala local de volume (0..1) aplicada além do volume da categoria")]
    [Range(0f, 1f)]
    public float localVolumeScale = 1f;

    private AudioSource source;
    private bool isRegistered = false;
    private Coroutine registerRoutine = null;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
    }

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            RegisterWithAudioManager();
        }
        else
        {
            // espera o AudioManager ficar disponível (evita warnings por ordem de inicialização)
            registerRoutine = StartCoroutine(RegisterWhenAudioManagerReady());
        }
    }

    private System.Collections.IEnumerator RegisterWhenAudioManagerReady()
    {
        float timeout = 5f;
        float t = 0f;
        while (AudioManager.Instance == null && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        registerRoutine = null;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AmbientEmitter: AudioManager.Instance não encontrado após espera. O som não será registrado.");
            yield break;
        }

        RegisterWithAudioManager();
    }

    private void RegisterWithAudioManager()
    {
        if (isRegistered) return;

        if (useSoundBankClip && !string.IsNullOrEmpty(soundName))
        {
            var clip = AudioManager.Instance.soundBank?.GetClip(soundName);
            if (clip != null)
            {
                source.clip = clip;
            }
            else
            {
                Debug.LogWarning($"AmbientEmitter: SoundBank não possui clip '{soundName}'. Usando clip do AudioSource (se houver)." );
            }
        }

        AudioManager.Instance.PlayLoopOnSource(source, soundName, category, Mathf.Clamp01(localVolumeScale));
        isRegistered = true;
        Debug.Log($"AmbientEmitter: registrado '{(string.IsNullOrEmpty(soundName) ? source.clip?.name ?? "(sem clip)" : soundName)}' no AudioManager.");
    }

    private void OnDisable()
    {
        if (registerRoutine != null)
        {
            StopCoroutine(registerRoutine);
            registerRoutine = null;
        }

        if (AudioManager.Instance != null && isRegistered)
        {
            AudioManager.Instance.StopLoopOnSource(source);
            isRegistered = false;
        }
    }

    private void OnDestroy()
    {
        if (registerRoutine != null)
        {
            StopCoroutine(registerRoutine);
            registerRoutine = null;
        }

        if (AudioManager.Instance != null && isRegistered)
        {
            AudioManager.Instance.StopLoopOnSource(source);
            isRegistered = false;
        }
    }
}
