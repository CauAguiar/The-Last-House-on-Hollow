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
        // Play optional SFX
        if (!string.IsNullOrEmpty(inspectSfx) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(inspectSfx, sfxCategory);
        }

        // Use base behavior (shows dialogue if inspectionText set)
        base.OnInspect();
    }
}
