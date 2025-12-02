using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Requires an AudioManager in the scene (for menu loop and typing SFX)

public class MainMenu : MonoBehaviour
{
    public Button[] botoesDoMenu;
    public GameObject iconeAbobora;

    private int selecaoAtual = 0;
    private float posXAbobora;

    [Header("Intro Panel")]
    public GameObject introPanel; // panel to fade in
    public TMP_Text introText; // text that will be typed
    [TextArea(4,10)]
    public string introFullText = "";
    public float charDelay = 0.03f;
    public float panelFadeIn = 1.0f;
    public float panelFadeOut = 1.0f;

    [Header("Menu Ambient")]
    public AudioSource menuLoopSource;
    public string menuLoopSoundName = "MenuAmbience";

    [Header("Typing SFX")]
    public string typingSfxName = "type_key";
    public AudioClip typingSfxClip;

    [Header("Scene")]
    public string sceneToLoad = "SalaoPrincipal";
    [Header("Editor Volume")]
    [Tooltip("Volume multiplier applied to the menu ambient loop (0..1). Useful to lower menu audio from Inspector.)")]
    [Range(0f,1f)] public float menuVolume = 1f;
    [Tooltip("Volume multiplier applied to the typing SFX loop (0..1).")]
    [Range(0f,1f)] public float typingVolume = 1f;
    private enum IntroState { Idle, Typing, Shown, FadingOut }
    private IntroState introState = IntroState.Idle;
    private Coroutine typingRoutine = null;
    private AudioSource typingLoopSource = null;
    [Header("Overlay")]
    [Tooltip("Optional fullscreen black overlay to show behind the intro panel during transition.")]
    public GameObject blackOverlay;
    private CanvasGroup blackOverlayCg;
    // Removed runtime UI slider fields - using Inspector volume multipliers instead

    void Start()
    {
        if (iconeAbobora != null)
        {
            var rt = iconeAbobora.GetComponent<RectTransform>();
            if (rt != null)
                posXAbobora = rt.anchoredPosition.x;
            else
                posXAbobora = iconeAbobora.transform.position.x;
        }

        AtualizarPosicaoAbobora();

        // Auto-wire pointer hover notifiers to menu buttons so hovering updates selection.
        if (botoesDoMenu != null)
        {
            for (int i = 0; i < botoesDoMenu.Length; i++)
            {
                var go = botoesDoMenu[i].gameObject;
                var notifier = go.GetComponent<ButtonHoverNotifier>();
                if (notifier == null)
                    notifier = go.AddComponent<ButtonHoverNotifier>();
                notifier.menu = this;
                notifier.index = i;
            }
        }

        // Ensure intro panel is hidden and has CanvasGroup
        if (introPanel != null)
        {
            var cg = introPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = introPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            introPanel.SetActive(false);
        }

        // Start menu ambient loop via AudioManager if configured. If AudioManager
        // is not available or the sound name fails to play, fall back to playing
        // the clip directly on the assigned AudioSource (if present).
        Debug.Log("MainMenu: Start() - AudioManager.Instance=" + (AudioManager.Instance != null) + ", menuLoopSource=" + (menuLoopSource != null) + ", menuLoopSoundName='" + menuLoopSoundName + "'");
        if (menuLoopSource != null)
        {
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(menuLoopSoundName))
            {
                AudioManager.Instance.PlayLoopOnSource(menuLoopSource, menuLoopSoundName, AudioManager.Category.Ambience, Mathf.Clamp01(menuVolume), true);
                Debug.Log("MainMenu: requested AudioManager to PlayLoopOnSource '" + menuLoopSoundName + "'");
            }
            else if (menuLoopSource.clip != null)
            {
                menuLoopSource.loop = true;
                menuLoopSource.volume = Mathf.Clamp01(menuVolume);
                menuLoopSource.Play();
                Debug.Log("MainMenu: fallback - playing menuLoopSource.clip directly ('" + menuLoopSource.clip.name + "')");
            }
        }

        // If no AudioSource was assigned earlier, try to create one so we can still play menu ambience.
        if (menuLoopSource == null)
        {
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(menuLoopSoundName))
            {
                var go = new GameObject("MenuLoopSource_Runtime");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = true;
                menuLoopSource = src;
                AudioManager.Instance.PlayLoopOnSource(menuLoopSource, menuLoopSoundName, AudioManager.Category.Ambience, Mathf.Clamp01(menuVolume), true);
                Debug.Log("MainMenu: created runtime AudioSource and requested AudioManager to PlayLoopOnSource '" + menuLoopSoundName + "'");
            }
            else
            {
                Debug.Log("MainMenu: no menuLoopSource assigned and no AudioManager available; menu ambience won't play.");
            }
        }
    }

    // Called by a UI click handler attached to the intro panel.
    // This avoids using the old Input API which conflicts with the new Input System package.
    public void OnIntroPanelClicked()
    {
        Debug.Log("MainMenu: OnIntroPanelClicked() called - state=" + introState + ", introPanelActive=" + (introPanel != null && introPanel.activeSelf));
        if (introPanel != null && introPanel.activeSelf && introState != IntroState.Idle)
        {
            IniciarJogo();
        }
    }

    public void DefinirSelecao(int index)
    {
        selecaoAtual = index;
        AtualizarPosicaoAbobora();
    }

    void AtualizarPosicaoAbobora()
    {
        if (iconeAbobora == null || botoesDoMenu == null || botoesDoMenu.Length == 0) return;
        if (selecaoAtual < 0 || selecaoAtual >= botoesDoMenu.Length) return;

        // If UI (RectTransform) use anchoredPosition for reliable alignment
        var iconRt = iconeAbobora.GetComponent<RectTransform>();
        var btnRt = botoesDoMenu[selecaoAtual].GetComponent<RectTransform>();
        if (iconRt != null && btnRt != null)
        {
            var anchored = iconRt.anchoredPosition;
            anchored.y = btnRt.anchoredPosition.y;
            iconRt.anchoredPosition = anchored;
        }
        else
        {
            // fallback to world Y
            float posYBotao = botoesDoMenu[selecaoAtual].transform.position.y;
            iconeAbobora.transform.position = new Vector3(posXAbobora, posYBotao, iconeAbobora.transform.position.z);
        }
    }

    public void IniciarJogo()
    {
        Debug.Log("MainMenu: IniciarJogo() called - current state=" + introState);
        // Handle multi-click behavior:
        // - Idle: start typing
        // - Typing: complete immediately
        // - Shown: start fade out and load
        if (introPanel == null || introText == null || string.IsNullOrEmpty(introFullText))
        {
            // fallback
            StopMenuLoop();
            SceneManager.LoadScene("SalaoPrincipalScene");
            return;
        }

        if (introState == IntroState.Idle)
        {
            Debug.Log("MainMenu: started typing intro");
            // start typing loop SFX (single looping source) if available
            if (!string.IsNullOrEmpty(typingSfxName) || typingSfxClip != null)
            {
                if (typingLoopSource == null)
                {
                    var go = new GameObject("TypingLoopSource");
                    go.transform.SetParent(transform);
                    typingLoopSource = go.AddComponent<AudioSource>();
                    typingLoopSource.playOnAwake = false;
                    typingLoopSource.loop = true;
                }
                if (AudioManager.Instance != null && !string.IsNullOrEmpty(typingSfxName))
                {
                    AudioManager.Instance.PlayLoopOnSource(typingLoopSource, typingSfxName, AudioManager.Category.UI, Mathf.Clamp01(typingVolume), true);
                }
                else if (typingSfxClip != null)
                {
                    typingLoopSource.clip = typingSfxClip;
                    typingLoopSource.loop = true;
                    typingLoopSource.volume = Mathf.Clamp01(typingVolume);
                    typingLoopSource.Play();
                }
            }
            typingRoutine = StartCoroutine(RunIntroAndLoad());
            introState = IntroState.Typing;
        }
        else if (introState == IntroState.Typing)
        {
            // complete immediately
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = null;
            introText.text = introFullText;
            introState = IntroState.Shown;
            Debug.Log("MainMenu: typing interrupted - showing full text");
            // stop typing loop if active
            if (typingLoopSource != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.StopLoopOnSource(typingLoopSource);
                else
                    typingLoopSource.Stop();
                Destroy(typingLoopSource.gameObject);
                typingLoopSource = null;
            }
        }
        else if (introState == IntroState.Shown)
        {
            // start fade out and load
            Debug.Log("MainMenu: starting fade out and load of scene '" + sceneToLoad + "'");
            StartCoroutine(FadeOutAndLoad());
        }
    }

    private void StopMenuLoop()
    {
        if (menuLoopSource != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopLoopOnSource(menuLoopSource);
            }
            else
            {
                menuLoopSource.Stop();
            }
        }
    }

    private IEnumerator RunIntroAndLoad()
    {
        // Fade in panel
        introPanel.SetActive(true);
        var cg = introPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = introPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
        // ensure black overlay exists but keep it hidden until fade-out
        EnsureBlackOverlay();
        if (blackOverlayCg != null)
        {
            blackOverlayCg.alpha = 0f;
            blackOverlay.SetActive(false);
        }
        float t = 0f;
        while (t < panelFadeIn)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / panelFadeIn);
            yield return null;
        }
        cg.alpha = 1f;

        // Typewriter effect with support for rich-text tags: when encountering
        // a '<' we append the whole tag immediately (no delay) so TMP doesn't
        // show raw tag characters while typing.
        introText.text = "";
        int i = 0;
        while (i < introFullText.Length)
        {
            if (introFullText[i] == '<')
            {
                int close = introFullText.IndexOf('>', i);
                if (close == -1)
                {
                    // malformed tag: append rest and break
                    introText.text += introFullText.Substring(i);
                    break;
                }
                // append the full tag immediately
                introText.text += introFullText.Substring(i, close - i + 1);
                i = close + 1;
                // don't play sfx or wait for tag characters
                continue;
            }

            // normal visible character
            introText.text += introFullText[i];
            i++;
            yield return new WaitForSecondsRealtime(charDelay);
        }

        // After typing finished, set state to Shown and wait for the player's next click to proceed
        typingRoutine = null;
        introState = IntroState.Shown;
        // stop typing loop if active
        if (typingLoopSource != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.StopLoopOnSource(typingLoopSource);
            else
                typingLoopSource.Stop();
            Destroy(typingLoopSource.gameObject);
            typingLoopSource = null;
        }
        yield break;
    }

    private IEnumerator FadeOutAndLoad()
    {
        introState = IntroState.FadingOut;
        var cg = introPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = introPanel.AddComponent<CanvasGroup>();
        float t = 0f;
        float start = cg.alpha;
        // enable overlay so it can fade-in while intro fades out
        EnsureBlackOverlay();
        if (blackOverlay != null)
        {
            blackOverlay.SetActive(true);
            if (blackOverlayCg != null) blackOverlayCg.alpha = 0f;
        }

        while (t < panelFadeOut)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / panelFadeOut));
            // fade overlay in opposite direction (0 -> 1)
            if (blackOverlayCg != null)
            {
                blackOverlayCg.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / panelFadeOut));
            }
            yield return null;
        }
        cg.alpha = 0f;

        // stop menu loop then load
        StopMenuLoop();
        Debug.Log("MainMenu: Fade finished. Loading scene '" + sceneToLoad + "'");
        // small delay to ensure audio stop and GC settle (optional)
        yield return null;
        // If there's an existing persistent AudioManager (from menu), destroy it
        // so the scene's own AudioManager can initialize as the singleton.
        if (AudioManager.Instance != null)
        {
            Debug.Log("MainMenu: Destroying existing persistent AudioManager before scene load.");
            var amGo = AudioManager.Instance.gameObject;
            // Clear static reference then destroy the GameObject
            AudioManager.Instance = null;
            Destroy(amGo);
        }

        SceneManager.LoadScene(sceneToLoad);
    }

    public void SairJogo()
    {
        Application.Quit();
        // Saindo do jogo (log removed)
    }

    // Slider callbacks
    // No runtime UI sliders: volume is controlled via Inspector fields `menuVolume` and `typingVolume`.

    private void OnDestroy()
    {
        // nothing to cleanup for removed UI sliders
    }

    private void EnsureBlackOverlay()
    {
        if (blackOverlay != null)
        {
            if (blackOverlayCg == null)
                blackOverlayCg = blackOverlay.GetComponent<CanvasGroup>();
            return;
        }

        // Parent overlay to the same parent as the introPanel when possible
        Transform parent = (introPanel != null && introPanel.transform.parent != null) ? introPanel.transform.parent : this.transform;

        var go = new GameObject("IntroBlackOverlay");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        img.raycastTarget = true;
        blackOverlayCg = go.AddComponent<CanvasGroup>();
        blackOverlayCg.alpha = 0f;

        // Place overlay behind the intro panel so intro stays on top
        if (introPanel != null)
        {
            int idx = introPanel.transform.GetSiblingIndex();
            go.transform.SetSiblingIndex(idx);
        }

        blackOverlay = go;
    }
}