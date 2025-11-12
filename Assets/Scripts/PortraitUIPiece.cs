using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PortraitUIPiece : MonoBehaviour
{
    public PortraitData data { get; private set; } 
    
    [SerializeField] private Image imageComponent; 
    [SerializeField] private Button buttonComponent; 
    [SerializeField] private TMP_Text labelText;

    public void Initialize(PortraitData portraitData)
    {
        this.data = portraitData;
        gameObject.name = "UI_Piece_" + portraitData.portraitID;

        // --- Atribuição de Componentes e Dados (Correto) ---
        if (imageComponent != null && portraitData.image != null)
        {
            imageComponent.sprite = portraitData.image;
        }
        else if (imageComponent != null)
        {
            imageComponent.color = Color.gray;
        }
        
        if (labelText != null)
        {
            labelText.text = portraitData.portraitID;
        }
       
        // O listener chama o método OnUIPieceSelected() do manager.
        if (buttonComponent != null)
        {
            // Remove qualquer listener antigo que possa ter sobrado para evitar duplicação.
            buttonComponent.onClick.RemoveAllListeners();

            buttonComponent.onClick.AddListener(() =>
            {
               
                Debug.Log($"[UIPiece] Clique registrado para: {portraitData.portraitID}");

                PortraitPuzzleController.Instance.OnUIPieceSelected(this);
            });
        }
        
        if (imageComponent != null)
{
    
    }
        }
 
}