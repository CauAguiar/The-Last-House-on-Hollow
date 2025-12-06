using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// UI simples para o Tomo: painel modal que mostra uma imagem com instruções.
/// Expor Show/Hide/Toggle para ser controlado por `QuickAccessButtons`.
/// </summary>
public class TomeUIManager : MonoBehaviour
{
    public static TomeUIManager Instance { get; private set; }

    [Header("Referências da UI")]
    [SerializeField] private GameObject tomoPanel;
    [SerializeField] private Image contentImage; // imagem que mostra as orientações
    [SerializeField] private Button closeButton;

    [Header("Configuração")]
    [Tooltip("Se verdadeiro, bloqueia movimento do jogador enquanto o Tomo estiver aberto")]
    [SerializeField] private bool lockPlayerWhileOpen = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
        // registra listener do botão fechar (se presente)
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }

    private void Start()
    {
        if (tomoPanel != null) tomoPanel.SetActive(false);
    }

    private void OnEnable()
    {
        Debug.Log($"TomeUIManager: OnEnable() tomoPanelAssigned={(tomoPanel!=null)} contentImageAssigned={(contentImage!=null)}");
    }

    public void Show()
    {
        if (tomoPanel == null) return;
        Debug.Log("TomeUIManager: Show() called");
        tomoPanel.SetActive(true);
        UIInputBlocker.Block("TomeUI");
        if (lockPlayerWhileOpen && PlayerMovement.Instance != null)
            PlayerMovement.Instance.LockMovement();
        if (EventSystem.current != null)
        {
            // seleciona o botão fechar para que controles de teclado/controle possam fechá-lo facilmente
            if (closeButton != null && closeButton.gameObject != null)
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            else
                EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void Hide()
    {
        if (tomoPanel == null) return;
        Debug.Log("TomeUIManager: Hide() called");
        tomoPanel.SetActive(false);
        UIInputBlocker.Unblock("TomeUI");
        if (lockPlayerWhileOpen && PlayerMovement.Instance != null)
            PlayerMovement.Instance.UnlockMovement();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
        }
    }

    public void Toggle()
    {
        if (tomoPanel == null) return;
        Debug.Log($"TomeUIManager: Toggle() called; currentlyActive={tomoPanel.activeSelf}");
        if (tomoPanel.activeSelf) Hide();
        else Show();
    }

    /// <summary>
    /// Retorna se o Tomo está atualmente visível.
    /// </summary>
    public bool IsTomeOpen()
    {
        return tomoPanel != null && tomoPanel.activeSelf;
    }

    /// <summary>
    /// Permite trocar dinamicamente a sprite exibida no Tomo.
    /// </summary>
    public void SetContentSprite(Sprite s)
    {
        if (contentImage != null)
        {
            contentImage.sprite = s;
            contentImage.enabled = s != null;
        }
    }
}
