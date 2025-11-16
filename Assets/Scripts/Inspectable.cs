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

        // Play optional SFX
        if (!string.IsNullOrEmpty(inspectSfx) && AudioManager.Instance != null)
        {
            if (startLoopOnInspect)
            {
                // Ensure there is an AudioSource to host the loop
                loopSource = GetComponent<AudioSource>();
                if (loopSource == null)
                {
                    loopSource = gameObject.AddComponent<AudioSource>();
                    loopSource.playOnAwake = false;
                    loopSource.loop = true;
                    loopSource.spatialBlend = 0f; // 2D by default for ambient loops
                }

                if (!loopStarted)
                {
                    AudioManager.Instance.PlayLoopOnSource(loopSource, inspectSfx, sfxCategory, Mathf.Clamp01(inspectSfxVolume));
                    loopStarted = true;
                }
            }
            else
            {
                AudioManager.Instance.PlaySFX(inspectSfx, sfxCategory, inspectSfxVolume);
            }
        }

        // Use base behavior (shows dialogue if inspectionText set)
        base.OnInspect();
    }

    private void OnDisable()
    {
        if (loopStarted && loopSource != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopOnSource(loopSource);
            loopStarted = false;
        }
    }

    private void OnDestroy()
    {
        if (loopStarted && loopSource != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopOnSource(loopSource);
            loopStarted = false;
        }
    }
}
