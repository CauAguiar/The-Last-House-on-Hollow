using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Small helper that closes a UI panel when the InputSystem "UI/Cancel" action is performed.
/// Attach this to any Canvas/Panel GameObject that should respond to Escape / Cancel.
/// It will prefer invoking the assigned closeButton (so the owner manager runs proper cleanup)
/// and otherwise will just deactivate the panel.
/// </summary>
public class UIAutoCloseOnCancel : MonoBehaviour
{
    [Tooltip("The panel to close. If unset, this GameObject will be used.")]
    public GameObject panel;

    [Tooltip("Optional close button to invoke. If present, its onClick listener will be invoked instead of simply deactivating.")]
    public Button closeButton;

    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
        if (panel == null) panel = this.gameObject;
    }

    private void OnEnable()
    {
        controls.UI.Enable();
        controls.UI.Cancel.performed += OnCancelPerformed;
    }

    private void OnDisable()
    {
        controls.UI.Cancel.performed -= OnCancelPerformed;
        controls.UI.Disable();
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (panel == null) return;
        if (!panel.activeSelf) return; // nothing to close

        // Prefer invoking the close button so the owning manager handles cleanup / unblocking / unpausing
        if (closeButton != null)
        {
            closeButton.onClick.Invoke();
            return;
        }

        panel.SetActive(false);
    }
}
