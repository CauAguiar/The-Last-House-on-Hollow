using UnityEngine;

/// <summary>
/// Controla o espelho, que revela uma pista quando o vapor é ativado.
/// </summary>
public class MirrorController : InteractableBase
{
    [Header("Configuração da Pista")]
    [Tooltip("O objeto de texto (TextMeshPro) que contém a pista e começa desativado.")]
    [SerializeField] private GameObject clueTextObject;
    
    [Tooltip("O que Charlie diz AO INSPECIONAR o espelho DEPOIS que a pista apareceu.")]
    [TextArea(2, 5)]
    [SerializeField] private string clueInspectionText;
    
    [Header("Controle de Estado")]
    [Tooltip("ID único para salvar o estado 'pista visível'.")]
    [SerializeField] private string uniqueId;
    
    private bool isClueVisible = false;

    [Header("Animação e Áudio da Revelação")]
    [Tooltip("CanvasGroup (opcional) para fazer fade da pista.")]
    [SerializeField] private CanvasGroup clueCanvasGroup;
    [Tooltip("Duração do fade/escala ao revelar a pista.")]
    [SerializeField] private float revealDuration = 0.6f;
    [Tooltip("Escala inicial (oculta) da pista antes do pop/fade.")]
    [SerializeField] private float hiddenScale = 0.6f;
    [Tooltip("Nome do som no SoundBank para tocar ao revelar a pista.")]
    [SerializeField] private string revealSoundName;
    [Tooltip("Categoria de áudio para o som da revelação.")]
    [SerializeField] private AudioManager.Category revealSoundCategory = AudioManager.Category.UI;

    private void Start()
    {
        // Verifica se a pista já estava visível
        isClueVisible = GameStateManager.Instance.IsCollected(uniqueId);
        clueTextObject.SetActive(isClueVisible);
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Método público chamado pelo SinkController para ativar a pista.
    /// </summary>
    public void ShowClue()
    {
        if (isClueVisible) return; // Não ativa duas vezes

        isClueVisible = true;
        PrepareRevealVisuals();
        clueTextObject.SetActive(true);
        StartCoroutine(RevealRoutine());
        GameStateManager.Instance.MarkAsCollected(uniqueId); // Salva o estado
    }

    /// <summary>
    /// Sobrescreve a lógica de inspeção.
    /// </summary>
    public override void OnInspect()
    {
        if (isClueVisible)
        {
            // Se a pista está visível, Charlie lê a pista.
            InteractionManager.Instance.ShowDialogue(clueInspectionText);
        }
        else
        {
            // Se não, ele apenas diz o texto de inspeção padrão da classe base.
            // Configure-o no Inspector com algo como "Apenas meu reflexo."
            base.OnInspect();
        }
    }

    // Bypass context menu: interacting should inspect/open directly
    public override void Interact()
    {
        OnInspect();
    }

    public override bool CanShowContextMenu()
    {
        return false;
    }

    private void PrepareRevealVisuals()
    {
        if (clueTextObject == null) return;
        var tr = clueTextObject.transform as RectTransform;
        if (tr != null)
        {
            tr.localScale = Vector3.one * hiddenScale;
        }
        if (clueCanvasGroup != null)
        {
            clueCanvasGroup.alpha = 0f;
        }
    }

    private System.Collections.IEnumerator RevealRoutine()
    {
        float t = 0f;
        var tr = clueTextObject.transform as RectTransform;
        // Toca som de revelação
        if (!string.IsNullOrEmpty(revealSoundName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(revealSoundName, revealSoundCategory, 1f);
        }
        while (t < revealDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / revealDuration);
            // Ease-out scale
            float scale = Mathf.Lerp(hiddenScale, 1f, 1f - Mathf.Pow(1f - normalized, 3f));
            if (tr != null) tr.localScale = Vector3.one * scale;
            if (clueCanvasGroup != null)
            {
                clueCanvasGroup.alpha = normalized;
            }
            yield return null;
        }
        if (tr != null) tr.localScale = Vector3.one;
        if (clueCanvasGroup != null) clueCanvasGroup.alpha = 1f;
    }
}