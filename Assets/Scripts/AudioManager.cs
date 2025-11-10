using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public SoundBank soundBank;


    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Efeitos Sonoros")]
    public AudioClip fireplace;

    // Cache de slices gerados para evitar recriação constante
    private System.Collections.Generic.Dictionary<string, AudioClip> sliceCache = new System.Collections.Generic.Dictionary<string, AudioClip>();

    [Header("Mix Global")]
    [Range(0f,1f)] public float masterVolume = 1f;
    [Range(0f,1f)] public float musicVolume = 1f;
    [Range(0f,1f)] public float sfxVolume = 1f;
    private const string PREF_MASTER = "vol_master";
    private const string PREF_MUSIC = "vol_music";
    private const string PREF_SFX = "vol_sfx";

    // Volumes por categoria
    public enum Category { SFX, Clock, UI, Ambience, Footsteps }
    private readonly System.Collections.Generic.Dictionary<Category, float> categoryVolumes = new System.Collections.Generic.Dictionary<Category, float>
    {
        { Category.SFX, 1f },
        { Category.Clock, 1f },
        { Category.UI, 1f },
        { Category.Ambience, 1f },
        { Category.Footsteps, 1f },
    };
    private const string PREF_CAT_PREFIX = "vol_cat_"; // e.g., vol_cat_Clock

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumes();
            ApplyVolumes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat(PREF_MASTER, 1f);
        musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC, 1f);
        sfxVolume = PlayerPrefs.GetFloat(PREF_SFX, 1f);
        foreach (Category cat in System.Enum.GetValues(typeof(Category)))
        {
            string key = PREF_CAT_PREFIX + cat.ToString();
            if (categoryVolumes.ContainsKey(cat))
                categoryVolumes[cat] = PlayerPrefs.GetFloat(key, 1f);
        }
    }

    private void SaveVolumes()
    {
        PlayerPrefs.SetFloat(PREF_MASTER, masterVolume);
        PlayerPrefs.SetFloat(PREF_MUSIC, musicVolume);
        PlayerPrefs.SetFloat(PREF_SFX, sfxVolume);
        foreach (var kv in categoryVolumes)
        {
            PlayerPrefs.SetFloat(PREF_CAT_PREFIX + kv.Key.ToString(), kv.Value);
        }
        PlayerPrefs.Save();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;
        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;
    }

    public void SetMasterVolume(float v){ masterVolume = Mathf.Clamp01(v); ApplyVolumes(); SaveVolumes(); }
    public void SetMusicVolume(float v){ musicVolume = Mathf.Clamp01(v); ApplyVolumes(); SaveVolumes(); }
    public void SetSfxVolume(float v){ sfxVolume = Mathf.Clamp01(v); ApplyVolumes(); SaveVolumes(); }
    public void SetCategoryVolume(Category cat, float v){ categoryVolumes[cat] = Mathf.Clamp01(v); SaveVolumes(); }
    public float GetCategoryVolume(Category cat){ return categoryVolumes.ContainsKey(cat) ? categoryVolumes[cat] : 1f; }

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
        ApplyVolumes();
        musicSource.Play();
    }


    // Reproduz um efeito sonoro gen?rico
    public void PlaySFX(string soundName)
    {
        AudioClip clip = soundBank.GetClip(soundName);
        if (clip != null)
        {
            ApplyVolumes(); // garante volume atualizado antes de tocar OneShot
            sfxSource.PlayOneShot(clip, masterVolume * sfxVolume);
        }
    }

    public void PlaySFX(string soundName, Category category, float volumeScale = 1f)
    {
        AudioClip clip = soundBank.GetClip(soundName);
        if (clip != null)
        {
            ApplyVolumes();
            float scale = Mathf.Clamp01(volumeScale) * masterVolume * sfxVolume * GetCategoryVolume(category);
            sfxSource.PlayOneShot(clip, scale);
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

        ApplyVolumes();
        sfxSource.PlayOneShot(sliceClip, Mathf.Clamp01(volume) * masterVolume * sfxVolume);
    }

    public void PlaySFXSlice(string soundName, float startSeconds, float durationSeconds, float volume, Category category)
    {
        AudioClip original = soundBank.GetClip(soundName);
        if (original == null) return;
        if (durationSeconds <= 0f || startSeconds < 0f)
        {
            PlaySFX(soundName, category, volume);
            return;
        }
        startSeconds = Mathf.Clamp(startSeconds, 0f, original.length);
        float endSeconds = Mathf.Clamp(startSeconds + durationSeconds, 0f, original.length);
        float sliceLength = Mathf.Max(0f, endSeconds - startSeconds);
        if (sliceLength <= 0.0001f) return;

        string key = soundName + "|" + startSeconds.ToString("F3") + "|" + sliceLength.ToString("F3");
        AudioClip sliceClip;
        if (!sliceCache.TryGetValue(key, out sliceClip))
        {
            int frequency = original.frequency;
            int channels = original.channels;
            int startSample = Mathf.RoundToInt(startSeconds * frequency);
            int sampleCount = Mathf.RoundToInt(sliceLength * frequency);
            float[] data = new float[sampleCount * channels];
            original.GetData(data, startSample);
            sliceClip = AudioClip.Create("slice_" + key, sampleCount, channels, frequency, false);
            sliceClip.SetData(data, 0);
            sliceCache[key] = sliceClip;
        }
        ApplyVolumes();
        float scale = Mathf.Clamp01(volume) * masterVolume * sfxVolume * GetCategoryVolume(category);
        sfxSource.PlayOneShot(sliceClip, scale);
    }

    // Nota: Footsteps são reproduzidos diretamente pelo PlayerMovement via PlaySFX com categoria Footsteps
}

