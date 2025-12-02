using UnityEngine;
using UnityEngine.EventSystems;

// Attach (or auto-add) to menu button GameObjects so hovering updates selection in MainMenu.
public class ButtonHoverNotifier : MonoBehaviour, IPointerEnterHandler
{
    public MainMenu menu;
    public int index = 0;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (menu != null)
        {
            menu.DefinirSelecao(index);
        }
    }
}
