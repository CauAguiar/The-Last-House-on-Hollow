using UnityEngine;

/// <summary>
/// Controlador de portas. Pode operar em dois modos:
/// - triggerMode (padrão): quando o jogador entra no trigger, carrega a cena automaticamente.
/// - interactionMode: o jogador precisa interagir (click/usar) com a porta; opcionalmente a porta pode estar trancada e requerer uma chave.
/// Esta implementação unifica comportamentos para evitar classes duplicadas.
/// </summary>
public class DoorController : InteractableBase
{
    [Header("Configuração da Porta")]
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string targetSpawnID;

    [Header("Modo de Operação")]
    [Tooltip("Se true, a porta só abre quando o jogador interage (menu de contexto). Caso contrário, a porta abre por trigger (OnTriggerEnter2D).")]
    [SerializeField] private bool requireInteraction = false;

    [Header("Tranca (opcional)")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private InventoryItem requiredKey;
    
    [Header("Persistência e Identificação")]
    [SerializeField]
    [Tooltip("ID único desta porta para persistência de estado entre cenas. Use 'Generate Unique ID' no menu de contexto do componente.")]
    private string uniqueId;

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    private void Start()
    {
        // Se esta porta já foi destrancada em uma sessão anterior, respeitamos esse estado.
        if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null && GameStateManager.Instance.IsDoorUnlocked(uniqueId))
        {
            isLocked = false;
        }
    }

#if UNITY_EDITOR
    // Garante que, em modo de edição, objetos recebam um uniqueId persistente automaticamente
    // quando o componente é adicionado ou o script recompila. Isso evita que IDs sejam gerados
    // apenas em Play Mode (que não persiste mudanças ao sair do modo Play).
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
            // Marca o objeto e a cena como sujos para que a mudança seja salva.
            UnityEditor.EditorUtility.SetDirty(this);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
    }
#endif

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Se a porta exige interação, normalmente ignoramos o trigger.
        // Porém, se a porta já estiver destrancada, permitimos que o jogador passe pelo trigger.
        if (requireInteraction && isLocked) return; // modo de interação só bloqueia triggers quando trancada

        // Verifica se o objeto que entrou no trigger tem a tag "Player"
        if (other.CompareTag("Player"))
        {
            // Se estiver trancada, não permite passar pelo trigger
            if (isLocked)
            {
                // Mostra uma dica curta ao jogador
                if (InteractionManager.Instance != null)
                {
                    InteractionManager.Instance.ShowDialogue("A porta está trancada.");
                }
                return;
            }

            // Salva a informação de para onde o jogador deve ir usando PlayerPrefs.
            PlayerPrefs.SetString("NextSpawnPointID", targetSpawnID);
            PlayerPrefs.Save(); // Garante que a informação seja salva imediatamente.

            // Chama o SceneLoader para carregar a cena com o efeito de fade.
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneLoader.Instance.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogWarning("DoorController: sceneToLoad não foi especificada.");
            }
        }
    }

    /// <summary>
    /// Interação via menu de contexto. Se a porta não estiver trancada, carrega a cena; se trancada, abre o menu (base.Interact) para permitir "Usar Item".
    /// </summary>
    public override void Interact()
    {
        // Em modos que não exigem interação, manter o comportamento padrão de contexto
        if (!requireInteraction)
        {
            base.Interact();
            return;
        }

        if (!isLocked)
        {
            // Carrega cena do mesmo jeito que o trigger faria
            PlayerPrefs.SetString("NextSpawnPointID", targetSpawnID);
            PlayerPrefs.Save();

            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneLoader.Instance.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogWarning("DoorController: sceneToLoad não foi especificada.");
            }
        }
        else
        {
            // Porta trancada: abre o menu de contexto para permitir usar item (OnUseItem)
            base.Interact();
        }
    }

    /// <summary>
    /// Tenta usar um item do inventário na porta. Se for a chave correta, destranca e consome o item.
    /// Caso contrário, delega para o comportamento base (mensagem de falha).
    /// </summary>
    public override void OnUseItem(InventoryItem item)
    {
        if (item == null)
        {
            base.OnUseItem(item);
            return;
        }

        if (requiredKey != null && item == requiredKey)
        {
            isLocked = false;

            // Persistir que a porta ficou destrancada
            if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null)
            {
                GameStateManager.Instance.MarkDoorUnlocked(uniqueId);
            }

            // Remove a chave do inventário
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveItem(requiredKey);
            }

            // Apenas destranca a porta e mostra feedback; o jogador deve entrar no colisor para atravessar.
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.ShowDialogue("A porta foi destrancada.");
            }
        }
        else
        {
            base.OnUseItem(item);
        }
    }
}


