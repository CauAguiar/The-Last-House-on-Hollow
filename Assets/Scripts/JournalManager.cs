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

    [Header("Item do Diário (opcional)")]
    [Tooltip("Se atribuído, quando este InventoryItem for adicionado ao inventário, a primeira página do diário será automaticamente desbloqueada.")]
    public InventoryItem journalItemReference;

    [Header("Páginas coletadas")]
    public List<int> collectedPages = new List<int>();

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
                Debug.Log("JournalManager: Item do diário coletado — primeira página adicionada automaticamente.");
            }
            else
            {
                Debug.LogWarning("JournalManager: journalData não está atribuído ou não contém páginas — não foi possível desbloquear a primeira página automaticamente.");
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
        if (journalData == null || journalData.pages == null)
        {
            Debug.LogWarning($"JournalManager.GetPageContent: journalData não atribuído. pageId={pageId}");
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
        Debug.LogWarning($"JournalManager: GetPageContent não encontrou pageId={pageId}. Available IDs: [{available}]. Trying fallback by index... ");

        // Fallback útil: alguns designers usam pageId como 1-based index. Se o pageId cair dentro do range 1..count, retornar pages[pageId-1]
        if (pageId >= 1 && pageId <= journalData.pages.Count)
        {
            Debug.LogWarning($"JournalManager: Usando fallback: retornando journalData.pages[{pageId - 1}] (pageId field={journalData.pages[pageId - 1].pageId}).");
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
                Debug.LogWarning($"JournalManager: CollectPage recebeu pageId=0. Mapeando para primeiro pageId do JournalData: {mapped}.");
                pageId = mapped;
            }
            else
            {
                Debug.LogWarning("JournalManager: CollectPage recebeu pageId=0 e não há journalData válido para mapear. Ignorando a adição de 0.");
                return;
            }
        }

        // 1. Garante que a página não será adicionada duas vezes
        if (!collectedPages.Contains(pageId))
        {
            collectedPages.Add(pageId);
            Debug.Log($"[JournalManager] Página {pageId} coletada com sucesso! CollectedPages now: [{string.Join(",", collectedPages)}]");

            // Log available page IDs for debugging
            if (journalData != null && journalData.pages != null)
            {
                var ids = new System.Text.StringBuilder();
                for (int i = 0; i < journalData.pages.Count; i++)
                {
                    if (i > 0) ids.Append(",");
                    ids.Append(journalData.pages[i].pageId);
                }
                Debug.Log($"[JournalManager] journalData available page IDs: [{ids}]");
            }

            // Ordena a lista de IDs para que a navegação seja sempre crescente (1, 2, 3...)
            collectedPages.Sort();
        }
        else
        {
            Debug.Log($"[JournalManager] A página {pageId} já havia sido coletada.");
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
        Debug.LogWarning($"[JournalManager] MODO DE TESTE: Todas as {collectedPages.Count} páginas foram adicionadas ao diário.");
    }

}