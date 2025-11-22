using UnityEngine;

/// <summary>
/// Holds global defaults for interactables (e.g., default cursor texture/hotspot).
/// Place a single GameObject with this component in the scene (or it will be created at runtime).
/// </summary>
public class InteractableDefaults : MonoBehaviour
{
    public static InteractableDefaults Instance { get; private set; }

    [Header("Cursor Defaults")]
    [Tooltip("Default cursor used when an InteractableBase does not specify a cursorHand.")]
    public Texture2D defaultCursor = null;
    [Tooltip("Hotspot in pixels for the default cursor.")]
    public Vector2 defaultHotspot = Vector2.zero;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
}
