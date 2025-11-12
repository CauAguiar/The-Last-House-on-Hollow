using UnityEngine;
using UnityEngine.UI;

public class Candle : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("ID numérico da vela (1 a 5).")]
    public int candleID; 

    [Header("Sprites")]
    public Sprite litSprite;    // Vela acesa
    public Sprite unlitSprite;  // Vela apagada

    private Image image;
    private Button button;
    private CandelabroController controller;

    void Awake()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    void Start()
    {
        controller = CandelabroController.Instance;
        // Liga a ação do clique
        button.onClick.AddListener(OnClick);
        ResetVisual();
    }

    private void OnClick()
    {
        // Se a vela já está acesa, ignore o clique
        if (image.sprite == litSprite) return; 

        // 1. Acende a vela visualmente
        SetLit(true); 
        
        // 2. Registra o clique no Controller
        if (controller != null)
        {
            controller.RegisterCandleClick(candleID);
        }
    }
    public void ResetVisual()
    {
        SetLit(false); // Chama o SetLit(false)
    }

    public void SetLit(bool isLit)
    {
        image.sprite = isLit ? litSprite : unlitSprite;
    }
}