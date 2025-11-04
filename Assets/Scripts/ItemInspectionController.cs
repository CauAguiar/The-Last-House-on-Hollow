using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador do painel de inspeção de itens do inventário.
/// Exibe informações detalhadas sobre um item selecionado.
/// </summary>
public class ItemInspectionController : MonoBehaviour
{
    public static ItemInspectionController Instance { get; private set; }

    [Header("UI do Painel de Inspeção")]
    [Tooltip("Painel principal da tela de inspeção")]
    public GameObject inspectionPanel;

    [Tooltip("Imagem que exibe o ícone do item")]
    public Image itemIconImage;

    [Tooltip("Texto que exibe o nome do item")]
    public TextMeshProUGUI itemNameText;

    [Tooltip("Texto que exibe a descrição do item")]
    public TextMeshProUGUI itemDescriptionText;

    [Tooltip("Botão para fechar o painel de inspeção")]
    public Button closeButton;

    private InventoryItem currentInspectedItem;

    private void Awake()
    {
        // Padrão Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Inicialmente o painel deve estar desativado
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(false);
        }

        // Configura o listener do botão de fechar
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInspection);
        }
    }

    /// <summary>
    /// Abre o painel de inspeção e exibe as informações do item.
    /// </summary>
    /// <param name="item">O item a ser inspecionado</param>
    public void ShowInspection(InventoryItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("Tentativa de inspecionar um item nulo!");
            return;
        }

        currentInspectedItem = item;

        // Popula os campos da UI com os dados do item
        if (itemIconImage != null)
        {
            if (item.icon != null)
            {
                itemIconImage.sprite = item.icon;
                itemIconImage.enabled = true;
            }
            else
            {
                itemIconImage.enabled = false;
            }
        }

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.description;
        }

        // Ativa o painel
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(true);
        }

        // Pausa o movimento do jogador (opcional mas recomendado)
        PausePlayerMovement(true);
    }

    /// <summary>
    /// Fecha o painel de inspeção e retorna ao inventário ou ao jogo.
    /// </summary>
    public void CloseInspection()
    {
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(false);
        }

        currentInspectedItem = null;

        // Despausa o movimento do jogador
        PausePlayerMovement(false);
    }

    /// <summary>
    /// Pausa ou despausa o movimento do jogador.
    /// </summary>
    /// <param name="pause">True para pausar, False para despausar</param>
    private void PausePlayerMovement(bool pause)
    {
        // Desabilita/Habilita o input do jogador através do PlayerControls
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.enabled = !pause;
        }

        // Alternativamente, poderia ser usado Time.timeScale = 0 para pausar completamente o jogo
        // mas isso pode afetar animações e outras mecânicas que não queremos pausar
        // Time.timeScale = pause ? 0f : 1f;
    }

    /// <summary>
    /// Retorna se o painel de inspeção está atualmente ativo.
    /// </summary>
    /// <returns>True se o painel estiver ativo, False caso contrário</returns>
    public bool IsInspectionOpen()
    {
        return inspectionPanel != null && inspectionPanel.activeSelf;
    }

    private void OnDestroy()
    {
        // Remove o listener ao destruir
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseInspection);
        }
    }
}
