using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Controla o progresso do jogador com as páginas do diário.
/// </summary>
public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance; // Singleton, para fácil acesso

    [Header("Referência ao ScriptableObject com as páginas")]
    public JournalData journalData;

    [Header("Item do Diário (opcional)")]
    [Tooltip("Se atribuído, quando este InventoryItem for adicionado ao inventário, a primeira página do diário será automaticamente desbloqueada.")]
    public InventoryItem journalItemReference;

    [Header("Páginas coletadas")]
    public List<int> collectedPages = new List<int>();
    // Evento disparado quando uma página é coletada (fornece o pageId)
    public event Action<int> OnPageCollected;

    private void Awake()
    {
        // Garante que só exista um JournalManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        // Registro de OnItemAdded é feito em OnEnable para evitar múltiplas inscrições.
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAdded += OnInventoryItemAdded;
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemAdded -= OnInventoryItemAdded;
    }

    private void OnInventoryItemAdded(InventoryItem item)
    {
        if (item == null || journalItemReference == null) return;

        // Se o item adicionado for o item do diário, garante que a primeira página esteja coletada
        if (item == journalItemReference)
        {
            if (journalData != null && journalData.pages != null && journalData.pages.Count > 0)
            {
                int firstPageId = journalData.pages[0].pageId;
                CollectPage(firstPageId);
            }
            else
            {
                // JournalManager: journalData não está atribuído ou não contém páginas — (log removed)
            }
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
        }
        else
        {
            // already collected (silenced log)
        }
    }

    /// <summary>
        /// Retorna o conteúdo da página com base no ID.
        /// </summary>
    public string GetPageContent(int pageId)
    {
        if (journalData == null || journalData.pages == null)
        {
            // JournalManager.GetPageContent: journalData não atribuído. (log removed)
            return "Página não encontrada.";
        }

        foreach (var page in journalData.pages)
        {
            if (page.pageId == pageId)
                return page.pageContent;
        }

        // Não encontrou por ID: loga os IDs disponíveis para ajudar a diagnosticar mismatch
        string available = "";
        for (int i = 0; i < journalData.pages.Count; i++)
        {
            available += (i == 0 ? "" : ", ") + journalData.pages[i].pageId;
        }
        // JournalManager: GetPageContent não encontrou pageId. (log removed)

        // Fallback útil: alguns designers usam pageId como 1-based index. Se o pageId cair dentro do range 1..count, retornar pages[pageId-1]
        if (pageId >= 1 && pageId <= journalData.pages.Count)
        {
            // JournalManager: usando fallback para retornar journalData.pages[...] (log removed)
            return journalData.pages[pageId - 1].pageContent;
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
        // Defensive: some Inspectable/DiaryPage might use 0 as a default value — try mapear 0 para o primeiro pageId conhecido
        if (pageId == 0)
        {
            if (journalData != null && journalData.pages != null && journalData.pages.Count > 0)
            {
                int mapped = journalData.pages[0].pageId;
                // JournalManager: CollectPage recebeu pageId=0. Mapeado para primeiro pageId (log removed)
                pageId = mapped;
            }
            else
            {
                // JournalManager: CollectPage recebeu pageId=0 e não há journalData válido para mapear (log removed)
                return;
            }
        }

        // 1. Garante que a página não será adicionada duas vezes
        if (!collectedPages.Contains(pageId))
        {
            collectedPages.Add(pageId);

            // Log available page IDs for debugging
            if (journalData != null && journalData.pages != null)
            {
                var ids = new System.Text.StringBuilder();
                for (int i = 0; i < journalData.pages.Count; i++)
                {
                    if (i > 0) ids.Append(",");
                    ids.Append(journalData.pages[i].pageId);
                }
                // [JournalManager] journalData available page IDs (log removed)
            }

            // Ordena a lista de IDs para que a navegação seja sempre crescente (1, 2, 3...)
            collectedPages.Sort();
            // Notifica ouvintes (UI, etc.) que uma página foi coletada
            try { OnPageCollected?.Invoke(pageId); } catch (Exception) { /* JournalManager: erro ao notificar OnPageCollected (log removed) */ }
        }
        else
        {
            // [JournalManager] A página já havia sido coletada. (log removed)
        }
    }

    [ContextMenu("TESTE: Coletar Todas as Páginas")]
    public void CollectAllPagesForTesting()
    {
        if (journalData == null)
        {
            Debug.LogError("[JournalManager] JournalData não está atribuído. Não é possível coletar as páginas.");
            return;
        }

        collectedPages.Clear(); // Limpa a lista antes de adicionar todas
        foreach (var page in journalData.pages)
        {
            if (!collectedPages.Contains(page.pageId))
            {
                collectedPages.Add(page.pageId);
            }
        }

        collectedPages.Sort(); // Ordena as páginas
        // [JournalManager] MODO DE TESTE: todas as páginas adicionadas (log removed)
    }

}