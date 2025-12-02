using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Attach this to the intro panel GameObject. This will ensure the panel
// has a Graphic (transparent Image) so it receives pointer events under
// the Input System UI Input Module. It forwards clicks to the assigned MainMenu.
[RequireComponent(typeof(RectTransform))]
public class IntroClickCatcher : MonoBehaviour, IPointerClickHandler
{
    public MainMenu menu;

    void Start()
    {
        // Ensure the panel has a Graphic (Image) component with RaycastTarget enabled
        // so it receives clicks from the UI Input Module. If the designer already
        // added a visible Image we don't change its color; otherwise we add a
        // transparent Image that only serves to catch pointer events.
        var img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
        }
        else
        {
            img.raycastTarget = true;
        }
        Debug.Log("IntroClickCatcher: initialized on '" + gameObject.name + "' (raycastTarget=" + img.raycastTarget + ")");

        // Also ensure there's a Button component so the UI system reliably sends
        // click events via OnClick. Some Input System setups are more reliable
        // with Button.OnClick listeners than raw pointer events.
        var btn = GetComponent<UnityEngine.UI.Button>();
        if (btn == null)
        {
            btn = gameObject.AddComponent<UnityEngine.UI.Button>();
            btn.transition = UnityEngine.UI.Selectable.Transition.None;
        }
        btn.onClick.RemoveAllListeners();
        if (menu != null)
        {
            btn.onClick.AddListener(() => { menu.OnIntroPanelClicked(); });
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("IntroClickCatcher: OnPointerClick detected on '" + gameObject.name + "'");
        if (menu != null)
        {
            menu.OnIntroPanelClicked();
        }
    }
}
