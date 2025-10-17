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

    public void PlayFootstep()
    {
        if (footstepSound != null)
            sfxSource.PlayOneShot(footstepSound);
    }
}

