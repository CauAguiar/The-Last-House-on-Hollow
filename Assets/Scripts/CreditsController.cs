using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Reflection;
using TMPro;

/// <summary>
/// Controls the credits sequence: fades TMP lines in/out one by one, then shows a final image (fade in/out),
/// and finally loads the Main Menu scene. Assumes AudioManager/global ambient loop is persistent.
/// Setup:
/// - Assign a `TMP_Text` to `creditsText` (it will be used to display each line).
/// - Add the credit lines to `creditsLines` (each entry is shown in order).
/// - Assign `finalImage` (UI Image) which must be initially disabled; it will be enabled for the final reveal.
/// - Configure fade timings and the `mainMenuSceneName`.
/// </summary>
public class CreditsController : MonoBehaviour
{
    [Header("UI refs")]
    public TMP_Text creditsText;
    public Image finalImage; // should be disabled at start

    [Header("Credits content")]
    [TextArea(2,6)]
    public List<string> creditsLines = new List<string>();

    [Header("Timing")]
    public float lineFadeIn = 1.0f;
    public float lineDisplay = 3.0f;
    public float lineFadeOut = 1.0f;

    [Header("Final image timing")]
    public float imageFadeIn = 1.5f;
    public float imageDisplay = 3.0f;
    public float imageFadeOut = 1.5f;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenuScene";

    private CanvasGroup finalImageGroup;

    private void Awake()
    {
        if (creditsText == null)
            Debug.LogError("CreditsController: creditsText is not assigned.");

        if (finalImage != null)
        {
            finalImageGroup = finalImage.GetComponent<CanvasGroup>();
            if (finalImageGroup == null)
            {
                finalImageGroup = finalImage.gameObject.AddComponent<CanvasGroup>();
            }
            finalImageGroup.alpha = 0f;
            finalImage.gameObject.SetActive(false);
        }

        if (creditsText != null)
        {
            var c = creditsText.color;
            c.a = 0f;
            creditsText.color = c;
            creditsText.text = "";
        }
    }

    private void Start()
    {
        StartCoroutine(RunCreditsSequence());
    }

    private IEnumerator RunCreditsSequence()
    {
        // Keep ambient music playing: AudioManager global loops are DontDestroyOnLoad, nothing to do here.

        // Show each line
        if (creditsText != null)
        {
            foreach (var line in creditsLines)
            {
                creditsText.text = line;
                yield return StartCoroutine(FadeTextAlpha(creditsText, 0f, 1f, lineFadeIn));
                yield return new WaitForSecondsRealtime(lineDisplay);
                yield return StartCoroutine(FadeTextAlpha(creditsText, 1f, 0f, lineFadeOut));
            }
            creditsText.text = "";
        }

        // Show final image with fade in/out
        if (finalImage != null)
        {
            finalImage.gameObject.SetActive(true);
            if (finalImageGroup == null) finalImageGroup = finalImage.GetComponent<CanvasGroup>();
            finalImageGroup.alpha = 0f;
            yield return StartCoroutine(FadeCanvasGroup(finalImageGroup, 0f, 1f, imageFadeIn));
            yield return new WaitForSecondsRealtime(imageDisplay);
            yield return StartCoroutine(FadeCanvasGroup(finalImageGroup, 1f, 0f, imageFadeOut));
            finalImage.gameObject.SetActive(false);
        }

        // Credits finished: perform cleanup of persistent singletons and load main menu
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            CleanupPersistentSingletons();
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void CleanupPersistentSingletons()
    {
        // List of common singleton type names to attempt cleanup on.
        string[] singletonNames = new string[] {
            "TheEndManager","AudioManager","LightManager","GameStateManager","InventoryManager",
            "InteractionManager","SceneLoader","PlayerController","PlayerMovement","UIExclusiveManager",
            "InventoryUIController","TypewriterUIManager","JournalManager","InteractableDefaults","QuickAccessButtons",
            "PickupNotificationManager"
        };

        foreach (var name in singletonNames)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(name);
                if (t == null) continue;

                // Try static field `Instance`
                var field = t.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                object inst = null;
                if (field != null)
                {
                    inst = field.GetValue(null);
                    if (inst is Object unityObj && unityObj != null)
                    {
                        try { Object.Destroy(((Component)unityObj).gameObject); } catch { try { Object.Destroy(unityObj); } catch {} }
                    }
                    // set field to null
                    try { field.SetValue(null, null); } catch {}
                }

                // Try static property `Instance` if property exists and not handled
                if (inst == null)
                {
                    var prop = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (prop != null)
                    {
                        try { inst = prop.GetValue(null, null); } catch { inst = null; }
                        if (inst is Object unityObj2 && unityObj2 != null)
                        {
                            try { Object.Destroy(((Component)unityObj2).gameObject); } catch { try { Object.Destroy(unityObj2); } catch {} }
                        }
                        // Try to set via setter if available
                        var set = prop.GetSetMethod(true);
                        if (set != null)
                        {
                            try { prop.SetValue(null, null, null); } catch {}
                        }
                        else if (field == null)
                        {
                            // No setter and no field: attempt to clear backing field by name
                            var backing = t.GetField("<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
                            if (backing != null)
                            {
                                try { backing.SetValue(null, null); } catch {}
                            }
                        }
                    }
                }
            }
        }

        // Additionally, try to destroy any root objects in the DontDestroyOnLoad scene (best-effort)
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var sc = SceneManager.GetSceneAt(i);
            if (!sc.isLoaded) continue;
            if (sc.name == "DontDestroyOnLoad")
            {
                var roots = sc.GetRootGameObjects();
                foreach (var r in roots)
                {
                    try { Object.Destroy(r); } catch {}
                }
            }
        }
    }

    private IEnumerator FadeTextAlpha(TMP_Text text, float from, float to, float duration)
    {
        if (text == null) yield break;
        float t = 0f;
        Color c = text.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float f = Mathf.Clamp01(t / duration);
            c.a = Mathf.Lerp(from, to, f);
            text.color = c;
            yield return null;
        }
        c.a = to;
        text.color = c;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float f = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, f);
            yield return null;
        }
        cg.alpha = to;
    }
}
