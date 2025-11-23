using UnityEngine;

/// <summary>
/// Gerencia painéis de UI que devem ser exclusivos — garante que apenas um painel
/// registrado esteja aberto por vez. Componentes de UI chamam PanelOpening(panel)
/// antes de ativar o painel e PanelClosed(panel) ao fechá-lo.
/// </summary>
public class UIExclusiveManager : MonoBehaviour
{
    public static UIExclusiveManager Instance { get; private set; }

    /// <summary>
    /// Retorna a instância atual do manager ou cria uma nova se não existir.
    /// Útil para garantir que a exclusividade funcione mesmo que o componente não tenha sido adicionado manualmente à cena.
    /// </summary>
    public static UIExclusiveManager GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("UIExclusiveManager");
        var mgr = go.AddComponent<UIExclusiveManager>();
        // Awake() será chamado automaticamente e preencherá Instance.
        return mgr;
    }

    private GameObject currentOpenPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad only works on root GameObjects.
            // Use the root of this transform to avoid the Unity warning when this component
            // is placed on a child object.
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Notifica que um painel está prestes a abrir. Fecha o painel atualmente aberto (se existir)
    /// antes de permitir que o novo abra.
    /// </summary>
    public void PanelOpening(GameObject panel)
    {
        if (panel == null) return;

        if (currentOpenPanel != null && currentOpenPanel != panel)
        {
            // Tenta fechar de forma segura: desativa o GameObject e envia mensagem OnExclusivePanelClosed
            currentOpenPanel.SetActive(false);
            currentOpenPanel.SendMessage("OnExclusivePanelClosed", SendMessageOptions.DontRequireReceiver);
        }

        currentOpenPanel = panel;
    }

    /// <summary>
    /// Notifica que um painel foi fechado — limpa referência se for o atual.
    /// </summary>
    public void PanelClosed(GameObject panel)
    {
        if (panel == null) return;
        if (currentOpenPanel == panel) currentOpenPanel = null;
    }

    /// <summary>
    /// Fecha qualquer painel aberto atualmente.
    /// </summary>
    public void CloseCurrent()
    {
        if (currentOpenPanel != null)
        {
            currentOpenPanel.SetActive(false);
            currentOpenPanel.SendMessage("OnExclusivePanelClosed", SendMessageOptions.DontRequireReceiver);
            currentOpenPanel = null;
        }
    }
}
