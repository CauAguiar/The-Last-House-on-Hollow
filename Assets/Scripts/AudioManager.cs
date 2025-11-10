using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public SoundBank soundBank;


    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Efeitos Sonoros")]
    public AudioClip footstepSound;
    public AudioClip fireplace;

    // Cache de slices gerados para evitar recriação constante
    private System.Collections.Generic.Dictionary<string, AudioClip> sliceCache = new System.Collections.Generic.Dictionary<string, AudioClip>();

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
        }
    }

    // Reproduz uma musica de fundo
    public void PlayMusic(string soundName)
    {
        AudioClip clip = soundBank.GetClip(soundName);
        if (clip == null)
        {
            Debug.LogWarning($"M?sica '{soundName}' n?o encontrada no SoundBank!");
            return;
        }

        if (musicSource.clip == clip)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }


    // Reproduz um efeito sonoro gen?rico
    public void PlaySFX(string soundName)
    {
        AudioClip clip = soundBank.GetClip(soundName);
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Abordagem precisa: cria (e cacheia) um subclip com somente o trecho desejado e toca via PlayOneShot.
    /// Não altera o 'sfxSource.clip' nem faz seek.
    /// </summary>
    public void PlaySFXSlice(string soundName, float startSeconds, float durationSeconds, float volume = 1f)
    {
        AudioClip original = soundBank.GetClip(soundName);
        if (original == null)
        {
            return;
        }

        if (durationSeconds <= 0f || startSeconds < 0f)
        {
            PlaySFX(soundName);
            return;
        }

        // Limita valores
        startSeconds = Mathf.Clamp(startSeconds, 0f, original.length);
        float endSeconds = Mathf.Clamp(startSeconds + durationSeconds, 0f, original.length);
        float sliceLength = Mathf.Max(0f, endSeconds - startSeconds);
        if (sliceLength <= 0.0001f)
        {
            return; // praticamente zero
        }

        string key = soundName + "|" + startSeconds.ToString("F3") + "|" + sliceLength.ToString("F3");
        AudioClip sliceClip;
        if (!sliceCache.TryGetValue(key, out sliceClip))
        {
            // Converte segundos para samples
            int frequency = original.frequency;
            int channels = original.channels;
            int startSample = Mathf.RoundToInt(startSeconds * frequency);
            int sampleCount = Mathf.RoundToInt(sliceLength * frequency);

            // Buffer para os dados da fatia (interleaved channels)
            float[] data = new float[sampleCount * channels];
            // Pega dados diretamente do clip original
            // offsetSamples é em "samples" já interleavados
            original.GetData(data, startSample);

            sliceClip = AudioClip.Create("slice_" + key, sampleCount, channels, frequency, false);
            sliceClip.SetData(data, 0);
            sliceCache[key] = sliceClip;
        }

        sfxSource.volume = volume;
        sfxSource.PlayOneShot(sliceClip);
    }

    public void PlayFootstep()
    {
        if (footstepSound != null)
            sfxSource.PlayOneShot(footstepSound);
    }
}

