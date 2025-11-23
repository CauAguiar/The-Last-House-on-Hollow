using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Mostra uma notificação (TMP) com efeito máquina de escrever quando uma página do diário é coletada,
/// e então a esconde com o efeito reverso. Também toca um SFX configurável.
/// Se várias páginas forem coletadas rapidamente, as notificações são enfileiradas.
/// </summary>
public class JournalNotificationUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI notificationText; // assign in inspector
    [Tooltip("Mensagem padrão; {0} será substituído pelo pageId se 'showPageId' estiver ativo.")]
    public string messageTemplate = "Diário atualizado!";
    public bool showPageId = true;

    [Header("Typewriter settings")]
    [Tooltip("Delay entre cada caractere (segundos)")]
    public float charDelay = 0.03f;
    [Tooltip("Tempo que a mensagem fica totalmente visível antes de começar o reverse (segundos)")]
    public float displayDuration = 1.5f;

    [Header("Audio (opcional)")]
    public AudioClip sfxClip;
    [Range(0f,1f)]
    public float sfxVolume = 1f;
    [Tooltip("Se true, usa spatialBlend (3D); se false, toca como UI (2D).")]
    public bool useSpatialSound = false;
    [Range(0f,1f)]
    public float spatialBlend = 0f; // 0 = 2D, 1 = 3D
    [Tooltip("Max distance for 3D rolloff (only used if useSpatialSound=true)")]
    public float maxDistance = 10f;
    [Tooltip("Min distance for 3D rolloff (only used if useSpatialSound=true)")]
    public float minDistance = 1f;

    // internal
    private Queue<string> queue = new Queue<string>();
    private bool isPlaying = false;
    private AudioSource audioSource;

    private void Awake()
    {
        if (notificationText == null)
        {
            Debug.LogError("JournalNotificationUI: notificationText não atribuído no Inspector.");
        }

        // Ensure an AudioSource exists for spatial playback when needed
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        ConfigureAudioSource();

        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (JournalManager.Instance != null)
        {
            JournalManager.Instance.OnPageCollected += HandlePageCollected;
        }
        else
        {
#if UNITY_2023_1_OR_NEWER
            var jm = UnityEngine.Object.FindFirstObjectByType<JournalManager>();
#else
            var jm = UnityEngine.Object.FindObjectOfType<JournalManager>();
#endif
            if (jm != null) jm.OnPageCollected += HandlePageCollected;
        }
    }

    private void OnDisable()
    {
        if (JournalManager.Instance != null)
        {
            JournalManager.Instance.OnPageCollected -= HandlePageCollected;
        }
        else
        {
#if UNITY_2023_1_OR_NEWER
            var jm = UnityEngine.Object.FindFirstObjectByType<JournalManager>();
#else
            var jm = UnityEngine.Object.FindObjectOfType<JournalManager>();
#endif
            if (jm != null) jm.OnPageCollected -= HandlePageCollected;
        }
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null) return;
        audioSource.volume = Mathf.Clamp01(sfxVolume);
        audioSource.spatialBlend = useSpatialSound ? spatialBlend : 0f;
        if (useSpatialSound)
        {
            audioSource.minDistance = Mathf.Max(0.01f, minDistance);
            audioSource.maxDistance = Mathf.Max(audioSource.minDistance + 0.01f, maxDistance);
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    private void HandlePageCollected(int pageId)
    {
        string msg = messageTemplate;
        if (showPageId) msg = string.Format("{0} (página {1})", messageTemplate, pageId);

        queue.Enqueue(msg);
        if (!isPlaying) StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isPlaying = true;
        while (queue.Count > 0)
        {
            string msg = queue.Dequeue();
            yield return StartCoroutine(PlayNotification(msg));
            // small gap between notifications
            yield return new WaitForSeconds(0.15f);
        }
        isPlaying = false;
    }

    private IEnumerator PlayNotification(string msg)
    {
        if (notificationText == null) yield break;

        notificationText.gameObject.SetActive(true);
        notificationText.text = "";

        // Play audio once when notification begins
        if (sfxClip != null && audioSource != null)
        {
            if (useSpatialSound)
            {
                audioSource.spatialBlend = spatialBlend;
                audioSource.minDistance = Mathf.Max(0.01f, minDistance);
                audioSource.maxDistance = Mathf.Max(audioSource.minDistance + 0.01f, maxDistance);
            }
            audioSource.PlayOneShot(sfxClip, sfxVolume);
        }

        // Typewriter forward
        int len = msg.Length;
        for (int i = 1; i <= len; i++)
        {
            notificationText.text = msg.Substring(0, i);
            yield return new WaitForSeconds(charDelay);
        }

        // Stay fully visible
        yield return new WaitForSeconds(displayDuration);

        // Reverse typewriter
        for (int i = len - 1; i >= 0; i--)
        {
            notificationText.text = msg.Substring(0, i);
            yield return new WaitForSeconds(charDelay);
        }

        notificationText.text = "";
        notificationText.gameObject.SetActive(false);
    }

    // Public API to update audio settings at runtime if needed
    public void SetSfxSettings(AudioClip clip, float volume, bool spatial, float blend = 0f, float minDist = 1f, float maxDist = 10f)
    {
        sfxClip = clip;
        sfxVolume = volume;
        useSpatialSound = spatial;
        spatialBlend = blend;
        minDistance = minDist;
        maxDistance = maxDist;
        ConfigureAudioSource();
    }
}
