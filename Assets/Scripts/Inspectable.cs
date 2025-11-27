using UnityEngine;

/// <summary>
/// Componente simples para objetos que só devem ser inspecionáveis e mostrar diálogo.
/// - Anexe em um GameObject com Collider2D e SpriteRenderer.
/// - Defina o texto de inspeção no campo `inspectionText` (herdado de InteractableBase).
/// - Por padrão, um clique abre o menu de contexto. Marque `directInspect` para que o clique mostre imediatamente o diálogo.
/// - Opcionalmente define um som para tocar ao inspecionar (usa AudioManager se estiver presente).
/// </summary>
public class Inspectable : InteractableBase
{
    [Header("Opções")]
    [Tooltip("Se verdadeiro, clicar no objeto chama diretamente OnInspect() sem abrir o menu de contexto.")]
    public bool directInspect = true;

    [Header("Áudio")]
    [Tooltip("Nome do som no SoundBank a tocar ao inspecionar (opcional)")]
    public string inspectSfx;

    public AudioManager.Category sfxCategory = AudioManager.Category.UI;

    [Header("Comportamento")]
    [Tooltip("Se verdadeiro, o objeto só pode ser inspecionado uma vez. Útil para objetos que iniciam loops/ações repetíveis.")]
    public bool inspectOnce = false;

    [Tooltip("Volume relativo do SFX (0..1) usado ao tocar o som de inspeção")]
    [Range(0f,1f)]
    public float inspectSfxVolume = 1f;

    [Tooltip("Se verdadeiro, o SFX será iniciado como loop no AudioSource do objeto (é necessário um AudioSource).")]
    public bool startLoopOnInspect = false;

    private bool hasBeenInspected = false;
    private bool loopStarted = false;
    private AudioSource loopSource;
    private bool loopRegisteredWithAudioManager = false;

    // Override Interact to either open the context menu (base) or inspect directly
    public override void Interact()
    {
        if (directInspect)
        {
            OnInspect();
        }
        else
        {
            base.Interact();
        }
    }

    public override void OnInspect()
    {
        if (inspectOnce && hasBeenInspected) return;

        // Mark as inspected before playing to avoid reentrancy
        if (inspectOnce) hasBeenInspected = true;

        // Play optional SFX (local to this GameObject / scene)
        if (!string.IsNullOrEmpty(inspectSfx))
        {
            // Prepare or reuse an AudioSource on this object
            if (loopSource == null)
            {
                loopSource = GetComponent<AudioSource>();
                if (loopSource == null)
                {
                    loopSource = gameObject.AddComponent<AudioSource>();
                    loopSource.playOnAwake = false;
                }
            }

            if (startLoopOnInspect)
            {
                // Loop via AudioManager registration when available (still plays on local AudioSource),
                // otherwise use the AudioClip directly on the local AudioSource.
                loopSource.loop = true;
                loopSource.spatialBlend = 0f; // keep 2D by default; set in inspector if you want 3D

                if (AudioManager.Instance != null && AudioManager.Instance.soundBank != null)
                {
                    // Try to use AudioManager to register loop so category volumes apply
                    AudioManager.Instance.PlayLoopOnSource(loopSource, inspectSfx, sfxCategory, Mathf.Clamp01(inspectSfxVolume));
                    loopRegisteredWithAudioManager = true;
                    loopStarted = true;
                }
                else
                {
                    // Fallback: try to get clip from sound bank if present, else do nothing
                    AudioClip clip = AudioManager.Instance?.soundBank?.GetClip(inspectSfx);
                    if (clip != null)
                    {
                        loopSource.clip = clip;
                        loopSource.volume = Mathf.Clamp01(inspectSfxVolume);
                        if (!loopSource.isPlaying)
                        {
                            loopSource.Play();
                            loopStarted = true;
                        }
                    }
                    else
                    {
                        // No clip available; nothing to play locally
                    }
                }
            }
            else
            {
                // One-shot: try to fetch clip from soundBank and play on this object's AudioSource
                AudioClip clip = AudioManager.Instance?.soundBank?.GetClip(inspectSfx);
                if (clip != null)
                {
                    loopSource.loop = false;
                    loopSource.spatialBlend = 0f;
                    loopSource.PlayOneShot(clip, Mathf.Clamp01(inspectSfxVolume));
                }
                else
                {
                    // If we cannot find the clip, avoid calling global AudioManager.PlaySFX to keep sound local-only
                    // Log a debug message so author can set up the sound bank properly
                    Debug.LogWarning($"Inspectable: sound '{inspectSfx}' not found in SoundBank; no local playback performed.", this);
                }
            }
        }

        // Use base behavior (shows dialogue if inspectionText set)
        base.OnInspect();
    }

    private void OnDisable()
    {
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
    }

    private void OnDestroy()
    {
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
    }
}
