using UnityEngine;

/// <summary>
/// Componente associado a cada prato do pentagrama. Recebe tentativas de uso de item
/// e notifica o `PentagramController` com o índice do prato. Herdando de
/// `InteractableBase` o objeto passa a ser interativo e exibirá o menu de contexto/inspeção.
/// </summary>
public class PlateController : InteractableBase
{
    [Tooltip("Índice deste prato dentro do PentagramController")]
    public int plateIndex = 0;

    [Header("Persistence")]
    [Tooltip("ID único opcional deste prato. Clique nos três pontinhos (...) do componente e escolha 'Generate Unique ID' para criar.")]
    [SerializeField] private string uniqueId = "";

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Generate Unique ID");
        UnityEditor.EditorUtility.SetDirty(this);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    [Tooltip("Referência opcional ao controlador do pentagrama. Se vazio, procurará no scene.")]
    public PentagramController pentagramController;

    protected override void Awake()
    {
        base.Awake();
        if (pentagramController == null)
        {
#if UNITY_2023_2_OR_NEWER
            pentagramController = UnityEngine.Object.FindAnyObjectByType<PentagramController>();
#else
            pentagramController = FindObjectOfType<PentagramController>();
#endif
        }
    }

    private bool interactable = true;

    /// <summary>
    /// Enable/disable interactivity for this plate. Disables Collider2D and prevents context menu.
    /// </summary>
    public void SetInteractable(bool enabled)
    {
        interactable = enabled;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = enabled;
        // reset visual hover state
        if (!enabled) transform.localScale = transform.localScale; // no-op but placeholder if needed
    }

    public override bool CanShowContextMenu()
    {
        return interactable && base.CanShowContextMenu();
    }

    /// <summary>
    /// Chamado pelo sistema de interação quando o jogador usa um item neste prato.
    /// </summary>
    public override void OnUseItem(InventoryItem item)
    {
        if (pentagramController == null)
        {
            Debug.LogWarning("PlateController: PentagramController não encontrado.");
            base.OnUseItem(item);
            return;
        }

        if (item == null)
        {
            // Se nenhum item foi passado, abrir o menu de contexto / inspeção
            base.Interact();
            return;
        }

        pentagramController.TryPlaceItem(plateIndex, item, this);
    }

    /// <summary>
    /// Chamado quando o jogador escolhe inspecionar o prato.
    /// </summary>
    public override void OnInspect()
    {
        // Use o comportamento padrão (mostra `inspectionText` via InteractionManager)
        base.OnInspect();
    }

    /// <summary>
    /// Feedback local para item incorreto. Recebe o item usado para permitir a
    /// exibição das frases contidas em `wrongItemResponses` (herdadas de InteractableBase).
    /// </summary>
    public void OnWrongItemUsed(InventoryItem item)
    {
        // Mostra a resposta padrão de uso incorreto via comportamento base
        base.OnUseItem(item);

        // Toca um SFX local opcional
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("wrong_item", AudioManager.Category.SFX, 0.8f);
        }
    }
}
