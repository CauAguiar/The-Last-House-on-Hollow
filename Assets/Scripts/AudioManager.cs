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
        // Atualiza fontes de loop registradas (ex: pia, lareira, etc.)
        foreach (var loop in loopSources)
        {
            if (loop.source == null) continue;
            float catVol = GetCategoryVolume(loop.category);
            loop.source.volume = masterVolume * sfxVolume * catVol * loop.localScale;
        }
    }

    public void SetMasterVolume(float v){ masterVolume = Mathf.Clamp01(v); ApplyVolumes(); SaveVolumes(); }
    public void SetMusicVolume(float v){ musicVolume = Mathf.Clamp01(v); ApplyVolumes(); SaveVolumes(); }
    public void SetSfxVolume(float v){ sfxVolume = Mathf.Clamp01(v); ApplyVolumes(); SaveVolumes(); }
    public void SetCategoryVolume(Category cat, float v){ categoryVolumes[cat] = Mathf.Clamp01(v); SaveVolumes(); }
    public float GetCategoryVolume(Category cat){ return categoryVolumes.ContainsKey(cat) ? categoryVolumes[cat] : 1f; }

    // Registro de loop sources para aplicar mix global automaticamente
    private struct LoopSourceInfo
    {
        public AudioSource source;
        public Category category;
        public float localScale;
        public string soundName;
    }
    private readonly System.Collections.Generic.List<LoopSourceInfo> loopSources = new System.Collections.Generic.List<LoopSourceInfo>();
    // An optional global AudioSource used to play cross-scene ambience immediately when needed
    private AudioSource globalLoopSource;
    private Coroutine globalFadeRoutine = null;

    /// <summary>
    /// Registra e inicia um loop contínuo (ex: fluxo de água da pia) com aplicação automática de volumes.
    /// </summary>
    public void PlayLoopOnSource(AudioSource targetSource, string soundName, Category category, float localScale = 1f, bool restartIfSame = false)
    {
        if (targetSource == null)
        {
            Debug.LogWarning("AudioManager.PlayLoopOnSource called with null targetSource");
            return;
        }

        AudioClip clip = null;
        if (soundBank == null)
        {
            Debug.LogWarning($"AudioManager: soundBank is null — cannot look up '{soundName}'. Falling back to targetSource.clip if available.");
        }
        else
        {
            clip = soundBank.GetClip(soundName);
            if (clip == null)
            {
                Debug.LogWarning($"AudioManager: sound '{soundName}' not found in SoundBank. Falling back to targetSource.clip if available.");
            }
        }

        // If we couldn't get a clip from the soundbank, try to use the AudioSource.clip as fallback
        if (clip == null)
        {
            if (targetSource.clip != null)
            {
                clip = targetSource.clip; // use existing clip
            }
            else
            {
                // Nothing to play
                Debug.LogWarning($"AudioManager: no clip available to play for '{soundName}' on target source.");
                return;
            }
        }
        if (!restartIfSame && targetSource.isPlaying && targetSource.clip == clip)
        {
            // Já está tocando o mesmo clip; apenas garante volume atualizado
            ApplyVolumes();
            return;
        }
        targetSource.clip = clip;
        targetSource.loop = true;
        targetSource.spatialBlend = 0f; // 2D por padrão para loops de UI/ambient internos
        // Calcula volume inicial
        float catVol = GetCategoryVolume(category);
        targetSource.volume = masterVolume * sfxVolume * catVol * Mathf.Clamp01(localScale);
        targetSource.Play();
        // Registra (remove caso já exista a mesma referência)
        loopSources.RemoveAll(ls => ls.source == targetSource);
        loopSources.Add(new LoopSourceInfo{ source = targetSource, category = category, localScale = Mathf.Clamp01(localScale), soundName = soundName });
    }

    /// <summary>
    /// Atualiza o volume de um loop já registrado (caso script externo ajuste localScale dinamicamente).
    /// </summary>
    public void RefreshLoopVolume(AudioSource targetSource, float newLocalScale)
    {
        for (int i = 0; i < loopSources.Count; i++)
        {
            if (loopSources[i].source == targetSource)
            {
                var info = loopSources[i];
                info.localScale = Mathf.Clamp01(newLocalScale);
                loopSources[i] = info;
                float catVol = GetCategoryVolume(info.category);
                if (info.source != null)
                    info.source.volume = masterVolume * sfxVolume * catVol * info.localScale;
                break;
            }
        }
    }

    /// <summary>
    /// Para e remove loop registrado.
    /// </summary>
    public void StopLoopOnSource(AudioSource targetSource)
    {
        loopSources.RemoveAll(ls => ls.source == targetSource);
        if (targetSource != null && targetSource.isPlaying)
            targetSource.Stop();
    }

    /// <summary>
    /// Stop all registered loop sources that are playing the specified sound name.
    /// </summary>
    public void StopLoopsByName(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        var toStop = loopSources.FindAll(ls => ls.soundName == soundName);
        foreach (var info in toStop)
        {
            if (info.source != null && info.source.isPlaying)
                info.source.Stop();
        }
        loopSources.RemoveAll(ls => ls.soundName == soundName);
    }

    /// <summary>
    /// Play a loop for the given sound name using a global AudioSource owned by the AudioManager.
    /// Useful to start ambient sounds immediately even if the emitter GameObject is not available yet.
    /// </summary>
    public void PlayLoopGlobal(string soundName, Category category, float localScale = 1f, float fadeDuration = 0.5f, float finalVolume = -1f)
    {
        if (string.IsNullOrEmpty(soundName) || soundBank == null) return;
        var clip = soundBank.GetClip(soundName);
        if (clip == null) return;

        if (globalLoopSource == null)
        {
            var go = new GameObject("GlobalLoopSource");
            go.transform.SetParent(this.transform);
            globalLoopSource = go.AddComponent<AudioSource>();
            globalLoopSource.loop = true;
            globalLoopSource.spatialBlend = 0f;
            DontDestroyOnLoad(go);
        }

        // prepare and play with initial volume 0, then fade to target
        float computedTarget = masterVolume * sfxVolume * GetCategoryVolume(category) * Mathf.Clamp01(localScale);
        float targetVol = finalVolume >= 0f ? Mathf.Clamp01(finalVolume) : computedTarget;
        globalLoopSource.clip = clip;
        globalLoopSource.loop = true;
        globalLoopSource.spatialBlend = 0f;
        globalLoopSource.volume = 0f;
        globalLoopSource.Play();

        // register in loopSources
        loopSources.RemoveAll(ls => ls.source == globalLoopSource);
        loopSources.Add(new LoopSourceInfo{ source = globalLoopSource, category = category, localScale = Mathf.Clamp01(localScale), soundName = soundName });

        // start fade coroutine
        if (globalFadeRoutine != null) StopCoroutine(globalFadeRoutine);
        if (fadeDuration > 0f)
            globalFadeRoutine = StartCoroutine(FadeVolumeCoroutine(globalLoopSource, targetVol, fadeDuration));
        else
            globalLoopSource.volume = targetVol;
    }

    private System.Collections.IEnumerator FadeVolumeCoroutine(AudioSource src, float target, float duration)
    {
        if (src == null) yield break;
        float start = src.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float f = Mathf.Clamp01(t / duration);
            src.volume = Mathf.Lerp(start, target, f);
            yield return null;
        }
        src.volume = target;
        globalFadeRoutine = null;
    }

    // Reproduz uma musica de fundo
    public void PlayMusic(string soundName)
    {
        AudioClip clip = soundBank.GetClip(soundName);
        if (clip == null)
        {
            // music not found in soundbank
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
        var sliceClip = GetOrCreateSlice(soundName, startSeconds, durationSeconds, out bool valid);
        if (!valid)
        {
            PlaySFX(soundName);
            return;
        }
        ApplyVolumes();
        sfxSource.PlayOneShot(sliceClip, Mathf.Clamp01(volume) * masterVolume * sfxVolume);
    }

    public void PlaySFXSlice(string soundName, float startSeconds, float durationSeconds, float volume, Category category)
    {
        var sliceClip = GetOrCreateSlice(soundName, startSeconds, durationSeconds, out bool valid);
        if (!valid)
        {
            PlaySFX(soundName, category, volume);
            return;
        }
        ApplyVolumes();
        float scale = Mathf.Clamp01(volume) * masterVolume * sfxVolume * GetCategoryVolume(category);
        sfxSource.PlayOneShot(sliceClip, scale);
    }

    /// <summary>
    /// Cria ou retorna de cache uma fatia (slice) de um clip do SoundBank.
    /// </summary>
    public AudioClip GetOrCreateSlice(string soundName, float startSeconds, float durationSeconds, out bool valid)
    {
        valid = false;
        AudioClip original = soundBank.GetClip(soundName);
        if (original == null) return null;
        if (durationSeconds <= 0f || startSeconds < 0f) return null;
        startSeconds = Mathf.Clamp(startSeconds, 0f, original.length);
        float endSeconds = Mathf.Clamp(startSeconds + durationSeconds, 0f, original.length);
        float sliceLength = Mathf.Max(0f, endSeconds - startSeconds);
        if (sliceLength <= 0.0001f) return null;
        string key = soundName + "|" + startSeconds.ToString("F3") + "|" + sliceLength.ToString("F3");
        if (!sliceCache.TryGetValue(key, out AudioClip sliceClip))
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
        valid = true;
        return sliceClip;
    }

    /// <summary>
    /// Inicia um loop usando apenas uma fatia do clip original (ex: parte contínua depois do som inicial).
    /// </summary>
    public void PlayLoopSliceOnSource(AudioSource targetSource, string soundName, float startSeconds, float durationSeconds, Category category, float localScale = 1f, bool restartIfSame = false)
    {
        if (targetSource == null || soundBank == null) return;
        var sliceClip = GetOrCreateSlice(soundName, startSeconds, durationSeconds, out bool valid);
        if (!valid || sliceClip == null)
        {
            // fallback: loop normal
            PlayLoopOnSource(targetSource, soundName, category, localScale, restartIfSame);
            return;
        }
        if (!restartIfSame && targetSource.isPlaying && targetSource.clip == sliceClip)
        {
            ApplyVolumes();
            return;
        }
        targetSource.clip = sliceClip;
        targetSource.loop = true;
        targetSource.spatialBlend = 0f;
        float catVol = GetCategoryVolume(category);
        targetSource.volume = masterVolume * sfxVolume * catVol * Mathf.Clamp01(localScale);
        targetSource.Play();
        // registra substituindo anterior
        loopSources.RemoveAll(ls => ls.source == targetSource);
        loopSources.Add(new LoopSourceInfo{ source = targetSource, category = category, localScale = Mathf.Clamp01(localScale), soundName = soundName + "|slice" });
    }

    // Nota: Footsteps são reproduzidos diretamente pelo PlayerMovement via PlaySFX com categoria Footsteps
}

