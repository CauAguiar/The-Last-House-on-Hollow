using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controla o progresso do jogador com as páginas do diário.
/// </summary>
public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance; // Singleton, para fácil acesso

    [Header("Referência ao ScriptableObject com as páginas")]
    public JournalData journalData;

    [Header("Páginas coletadas")]
    public List<int> collectedPages = new List<int>();

    private void Awake()
    {
        // Garante que só exista um JournalManager
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Marca uma página como coletada pelo jogador.
    /// </summary>
    public void AddPage(int pageId)
    {
        if (!collectedPages.Contains(pageId))
        {
            collectedPages.Add(pageId);
            Debug.Log($"Página {pageId} adicionada ao diário!");
        }
        else
        {
            Debug.Log($"A página {pageId} já foi coletada.");
        }
    }

    /// <summary>
    /// Retorna o conteúdo da página com base no ID.
    /// </summary>
    public string GetPageContent(int pageId)
    {
        foreach (var page in journalData.pages)
        {
            if (page.pageId == pageId)
                return page.pageContent;
        }

        return "Página não encontrada.";
    }

    /// <summary>
    /// Verifica se uma página específica já foi coletada.
    /// </summary>
    public bool HasPage(int pageId)
    {
        return collectedPages.Contains(pageId);
    }


    /// <summary>
/// Chamado pelo DiaryPage quando o jogador coleta uma página.
/// </summary>
public void CollectPage(int pageId) 
{
    // 1. Garante que a página não será adicionada duas vezes
    if (!collectedPages.Contains(pageId))
    {
        collectedPages.Add(pageId);
        Debug.Log($"[JournalManager] Página {pageId} coletada com sucesso!");
        
        // Ordena a lista de IDs para que a navegação seja sempre crescente (1, 2, 3...)
        collectedPages.Sort(); 
    }
    else
    {
        Debug.Log($"[JournalManager] A página {pageId} já havia sido coletada.");
    }
}

}