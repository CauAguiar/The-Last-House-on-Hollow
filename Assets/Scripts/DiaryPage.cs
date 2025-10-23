using UnityEngine;

/// <summary>
/// Representa uma página do diário que pode ser coletada pelo jogador.
/// </summary>
public class DiaryPage : MonoBehaviour, IInteractable
{
    [Header("Configurações da Página")]
    [TextArea]
    public string pageText; // Texto da página
    public int pageID = 0;  // ID único da página (para identificar no JournalManager)

    [Header("Feedbacks Visuais e Sonoros")]
    public AudioClip pickupSfx;
    public bool destroyOnCollect = false;

    // ----------------------------------------------------------
    // MÉTODOS EXIGIDOS PELA INTERFACE IInteractable
    // ----------------------------------------------------------

    /// <summary>

    /// </summary>
    [ContextMenu("TESTE: Coletar Página")]
    public void Interact()
    {
        //Tenta encontrar o JournalManager na cena
        var jm = JournalManager.Instance;


        if (jm != null)
        {
            // 2. Chama CollectPage APENAS com o ID
            jm.CollectPage(pageID);
            Debug.Log($"[DiaryPage] Página {pageID} coletada!");
        }
        // Remove ou desativa o objeto
        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Chamado quando o jogador entra no raio de proximidade.
    /// </summary>
    public void OnProximityEnter()
    {
        Debug.Log($"[DiaryPage] Jogador está próximo da página {pageID}.");

    }

    /// <summary>
    /// Chamado quando o jogador sai do raio de proximidade.
    /// </summary>
    public void OnProximityExit()
    {
        Debug.Log($"[DiaryPage] Jogador saiu da proximidade da página {pageID}.");

    }
}
