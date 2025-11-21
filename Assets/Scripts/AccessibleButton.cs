using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

/// <summary>
/// Small helper to provide accessible name metadata for buttons. It will set a descriptive GameObject name
/// based on the label text so automated tools and logging can identify the button usefully.
/// Only a minimal step towards screen reader support.
/// </summary>
[RequireComponent(typeof(Button))]
public class AccessibleButton : MonoBehaviour
{
    [Tooltip("Text description for accessibility purposes. If empty, tries to get text from child TMP/Text component.")]
    public string accessibleName;

    private void Awake()
    {
        if (string.IsNullOrEmpty(accessibleName))
        {
#if TMP_PRESENT
            var tmp = GetComponentInChildren<TMP_Text>();
            if (tmp != null) accessibleName = tmp.text;
            else
#endif
            {
                var txt = GetComponentInChildren<UnityEngine.UI.Text>();
                if (txt != null) accessibleName = txt.text;
            }
        }

        if (!string.IsNullOrEmpty(accessibleName))
        {
            // Prefix to keep it obvious in hierarchy
            gameObject.name = "Button_" + accessibleName.Replace(' ', '_');
        }
    }
}