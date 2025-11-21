using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Simple focus trap for modal dialogs: keeps keyboard Tab navigation inside the modal.
/// Attach to a panel that contains focusable Selectable components (Buttons, Toggles, InputFields, etc.).
/// Works with both the old Input and the new InputSystem's Tab key.
/// </summary>
public class UIFocusTrap : MonoBehaviour
{
    private Selectable[] selectables;

    private void OnEnable()
    {
        RefreshSelectables();
        // If nothing selected, pick the first
        var es = EventSystem.current;
        if (es != null && (es.currentSelectedGameObject == null || !IsInsideSelectables(es.currentSelectedGameObject)))
        {
            if (selectables.Length > 0 && selectables[0] != null)
                es.SetSelectedGameObject(selectables[0].gameObject);
        }
    }

    private void RefreshSelectables()
    {
        selectables = GetComponentsInChildren<Selectable>(true);
    }

    private bool IsInsideSelectables(GameObject go)
    {
        for (int i = 0; i < selectables.Length; i++)
            if (selectables[i] != null && selectables[i].gameObject == go) return true;
        return false;
    }

    private void Update()
    {
        if (selectables == null || selectables.Length == 0) return;
        var es = EventSystem.current;
        if (es == null) return;

        // Ensure selection stays inside
        if (es.currentSelectedGameObject != null && !IsInsideSelectables(es.currentSelectedGameObject))
        {
            es.SetSelectedGameObject(selectables[0].gameObject);
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
#else
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Tab))
#endif
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            bool shift = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
#else
            bool shift = UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftShift) || UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift);
#endif
            MoveSelection(shift ? -1 : 1);
        }
    }

    private void MoveSelection(int delta)
    {
        var es = EventSystem.current;
        if (es == null) return;
        GameObject current = es.currentSelectedGameObject;
        int idx = -1;
        for (int i = 0; i < selectables.Length; i++)
        {
            if (selectables[i] != null && selectables[i].gameObject == current) { idx = i; break; }
        }

        if (idx == -1)
        {
            es.SetSelectedGameObject(selectables[0].gameObject);
            return;
        }

        int next = (idx + delta + selectables.Length) % selectables.Length;
        es.SetSelectedGameObject(selectables[next].gameObject);
    }
}