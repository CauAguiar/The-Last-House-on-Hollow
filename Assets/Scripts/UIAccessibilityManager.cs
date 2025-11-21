using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Global accessibility helper for text scaling and simple themes.
/// Attach to a persistent GameObject (e.g., GameManager) and call Apply* functions from a Settings UI.
/// </summary>
public class UIAccessibilityManager : MonoBehaviour
{
    public static UIAccessibilityManager Instance { get; private set; }

    [Range(0.6f, 2.0f)]
    public float textScale = 1.0f;

    private const string PREF_KEY_TEXT_SCALE = "UI_TextScale";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        textScale = PlayerPrefs.GetFloat(PREF_KEY_TEXT_SCALE, 1f);
    }

    /// <summary>
    /// Applies the configured text scale to all TextMeshProUGUI elements found in the active scene(s).
    /// This is a simple baseline and will multiply each component's fontSize by the scale value.
    /// </summary>
    public void ApplyTextScale()
    {
        // Use the newer FindObjectsByType when available to avoid deprecated warnings
    #if UNITY_2023_1_OR_NEWER
        var texts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
    #else
        var texts = FindObjectsOfType<TextMeshProUGUI>(true);
    #endif
        foreach (var t in texts)
        {
            // We store baseline size in name to avoid modifying it repeatedly.
            if (!t.gameObject.TryGetComponent<BaselineTextSize>(out var baseline))
            {
                baseline = t.gameObject.AddComponent<BaselineTextSize>();
                baseline.value = t.fontSize;
            }
            t.fontSize = baseline.value * textScale;
        }
        PlayerPrefs.SetFloat(PREF_KEY_TEXT_SCALE, textScale);
        PlayerPrefs.Save();
    }

    [System.Serializable]
    public class BaselineTextSize : MonoBehaviour
    {
        public float value = 14f;
    }
}