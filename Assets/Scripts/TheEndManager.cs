using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_2020_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#else
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// Manager persistente que aplica ações finais (ativar objetos, trocar sprites) em cenas alvo
/// quando um evento "fim" é disparado (ex: Pentagram resolvido). Modelo baseado no LightManager.
/// Coloque este componente em um GameObject na cena principal e marque-o como persistente.
/// </summary>
public class TheEndManager : MonoBehaviour
{
    public static TheEndManager Instance { get; private set; }

    [Header("Target scene / objects")]
    [Tooltip("Nome da cena que receberá ações (ex: 'SalaoPrincipal')")]
    public string targetSceneName = "SalaoPrincipal";

    [Tooltip("Nomes dos GameObjects na cena alvo a ativar quando o evento for disparado")]
    public string[] targetObjectsToEnable = new string[] { "Saida", "SaidaLuz" };

    [Header("Tim Craven (Porão)")]
    [Tooltip("Nome da cena onde o objeto do TimCraven deverá ser ocultado (ex: 'PoraoScene')")]
    public string timCravenSceneName = "PoraoScene";
    [Tooltip("Nome do GameObject do NPC TimCraven a ocultar na cena do porão")]
    public string timCravenObjectName = "TimCraven";

    [Header("Fireplace (SalaoPrincipal)")]
    [Tooltip("Nome do GameObject da lareira na cena alvo cuja sprite será trocada/desativada a luz")]
    public string fireplaceObjectName = "Lareira";
    [Tooltip("Sprite a aplicar na lareira (opcional)")]
    public Sprite fireplaceNewSprite;

    [Header("Ambient emitters")]
    [Tooltip("Nomes dos GameObjects de ambiente a desativar na cena alvo")]
    // Prefer references (assign GameObjects in the inspector) similar to LightManager.
    public GameObject[] ambientToDisable = new GameObject[0];
    [Tooltip("(LightManager-style) Single ambient GameObject to disable when the end triggers")]
    public GameObject ambientToDisableSingle;
    [Tooltip("(LightManager-style) Single ambient GameObject to enable when the end triggers")]
    public GameObject ambientToEnableSingle;
    [Tooltip("Nome do GameObject de ambiente a ativar na cena alvo (fallback por nome se ambientToEnableRef não preenchido)")]
    public GameObject ambientToEnableRef;
    [Tooltip("Fallback: nome do ambient a ativar se não usar refs")] 
    public string ambientObjectToEnable = "";
    [Tooltip("Fade duration (seconds) when starting the replacement ambient sound")]
    public float ambientFadeDuration = 6.0f;
    [Tooltip("Final target volume (0..1) for the replacement ambient when faded in")]
    [Range(0f,1f)]
    public float ambientTargetVolume = 0.5f;

    [Header("End Dialogues")]
    [Tooltip("Linhas de diálogo a serem exibidas quando o pentagrama é resolvido")]
    public System.Collections.Generic.List<string> endDialogues = new System.Collections.Generic.List<string>();
    [Tooltip("Se verdadeiro, o diálogo avança automaticamente após a digitação")]
    public bool endDialoguesAutoAdvance = true;
    [Tooltip("Delay (segundos) após a digitação para avançar automaticamente")]
    public float endDialoguesAutoAdvanceDelay = 0.7f;

    // Estado pendente para aplicar ações quando a cena alvo for carregada
    private bool pendingSceneActions = false;
    // Marca que as ações para a cena alvo já foram disparadas (por exemplo, após o pentagrama)
    private bool sceneActionsCompleted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Discover current loaded scenes if needed (no auto-discovery of objects)
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!(pendingSceneActions || sceneActionsCompleted)) return;

        if (!string.IsNullOrEmpty(targetSceneName) && string.Equals(scene.name, targetSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            ApplySceneActions(scene);
            return;
        }

        if (!string.IsNullOrEmpty(timCravenSceneName) && string.Equals(scene.name, timCravenSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            ApplySceneActions(scene);
            return;
        }
    }

    /// <summary>
    /// Dispare o evento de fim. Normalmente chamado por PentagramController quando o puzzle for resolvido.
    /// </summary>
    public void TriggerEnd(string uniqueId = "")
    {
        sceneActionsCompleted = true;
        Debug.Log($"TheEndManager: TriggerEnd called (uniqueId='{uniqueId}'). Trying to apply or schedule actions for scene '{targetSceneName}'.");

        // Try apply immediately if scene loaded
        bool appliedAny = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var sc = SceneManager.GetSceneAt(i);
            if (!sc.isLoaded) continue;
            if (string.Equals(sc.name, targetSceneName, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sc.name, timCravenSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"TheEndManager: scene '{sc.name}' is already loaded. Applying actions now.");
                ApplySceneActions(sc);
                appliedAny = true;
            }
        }

        // Run immediate post-end effects (mirrors LightManager: disable one ambient and enable another)
        // First: stop ambient audio by name for any configured ambientToDisable entries
        if (AudioManager.Instance != null)
        {
            // stop single reference first
            if (ambientToDisableSingle != null)
            {
                var emitter = ambientToDisableSingle.GetComponent<AmbientEmitter>();
                if (emitter != null && !string.IsNullOrEmpty(emitter.soundName))
                {
                    AudioManager.Instance.StopLoopsByName(emitter.soundName);
                    Debug.Log($"TheEndManager: (immediate) StopLoopsByName('{emitter.soundName}') called.");
                }
            }

            // stop array-configured ambients
            if (ambientToDisable != null && ambientToDisable.Length > 0)
            {
                foreach (var ambRef in ambientToDisable)
                {
                    if (ambRef == null) continue;
                    var emitter = ambRef.GetComponent<AmbientEmitter>();
                    if (emitter != null && !string.IsNullOrEmpty(emitter.soundName))
                    {
                        AudioManager.Instance.StopLoopsByName(emitter.soundName);
                        Debug.Log($"TheEndManager: (immediate) StopLoopsByName('{emitter.soundName}') called.");
                    }
                }
            }

            // If ambientToEnableSingle has an AmbientEmitter, try to start its sound immediately via global player
            if (ambientToEnableSingle != null)
            {
                var emitterOn = ambientToEnableSingle.GetComponent<AmbientEmitter>();
                if (emitterOn != null && !string.IsNullOrEmpty(emitterOn.soundName))
                {
                    // Start with a longer fade-in to avoid abrupt start and reach target volume
                    AudioManager.Instance.PlayLoopGlobal(emitterOn.soundName, emitterOn.category, emitterOn.localVolumeScale, ambientFadeDuration, ambientTargetVolume);
                    Debug.Log($"TheEndManager: (immediate) PlayLoopGlobal('{emitterOn.soundName}') called with fade {ambientFadeDuration}s to volume {ambientTargetVolume}.");
                }
            }
        }

        StartCoroutine(ImmediatePostEndEffectsRoutine());

        // If none applied because scenes not loaded yet, schedule for next load
        if (!appliedAny)
        {
            pendingSceneActions = true;
            Debug.Log($"TheEndManager: actions for scenes scheduled (will apply when scene loads).");
        }

        // Start end dialogues (if any)
        if (endDialogues != null && endDialogues.Count > 0)
        {
            StartCoroutine(PlayEndDialogues());
        }
    }

    private void ApplySceneActions(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        Debug.Log($"TheEndManager: ApplySceneActions called for scene '{scene.name}'. Looking for targets: { (targetObjectsToEnable != null ? string.Join(",", targetObjectsToEnable) : "(none)") }");

        var roots = scene.GetRootGameObjects();

        // Enable listed objects
        foreach (var name in targetObjectsToEnable)
        {
            if (string.IsNullOrEmpty(name)) continue;
            GameObject found = null;
            foreach (var root in roots)
            {
                var transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t == null) continue;
                    if (t.name == name)
                    {
                        found = t.gameObject; break;
                    }
                }
                if (found != null) break;
            }
            if (found != null)
            {
                Debug.Log($"TheEndManager: found '{name}' (activeSelf={found.activeSelf}, rootActive={found.transform.root.gameObject.activeSelf}) in scene '{scene.name}' at path {found.GetPath()}");
                var rootGo = found.transform.root.gameObject;
                if (!rootGo.activeSelf)
                {
                    Debug.Log($"TheEndManager: root '{rootGo.name}' is inactive; enabling root so '{name}' becomes active.");
                    rootGo.SetActive(true);
                    Debug.Log($"TheEndManager: root '{rootGo.name}' activeSelf now={rootGo.activeSelf}");
                }
                found.SetActive(true);
                Debug.Log($"TheEndManager: enabled '{name}' in scene '{scene.name}' -> now activeSelf={found.activeSelf}");
            }
            else
            {
                Debug.LogWarning($"TheEndManager: could not find '{name}' in scene '{scene.name}' to enable.");
            }
        }

        // If this scene is the porão (timCravenScene), hide TimCraven
        if (!string.IsNullOrEmpty(timCravenSceneName) && string.Equals(scene.name, timCravenSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(timCravenObjectName))
            {
                GameObject timObj = null;
                foreach (var root in roots)
                {
                    var transforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in transforms)
                    {
                        if (t == null) continue;
                        if (t.name == timCravenObjectName)
                        {
                            timObj = t.gameObject; break;
                        }
                    }
                    if (timObj != null) break;
                }

                if (timObj != null)
                {
                    timObj.SetActive(false);
                    Debug.Log($"TheEndManager: Hid TimCraven ('{timCravenObjectName}') in scene '{scene.name}'.");
                }
                else
                {
                    Debug.LogWarning($"TheEndManager: Não encontrou TimCraven ('{timCravenObjectName}') na cena '{scene.name}'.");
                }
            }
        }

        // If this scene is the target scene (SalaoPrincipal), perform enabling, fireplace swap and ambient toggles
        if (!string.IsNullOrEmpty(targetSceneName) && string.Equals(scene.name, targetSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            // Optional fireplace sprite swap + disable light on that object
            if (!string.IsNullOrEmpty(fireplaceObjectName))
            {
                GameObject fireObj = null;
                foreach (var root in roots)
                {
                    var transforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in transforms)
                    {
                        if (t == null) continue;
                        if (t.name == fireplaceObjectName)
                        {
                            fireObj = t.gameObject; break;
                        }
                    }
                    if (fireObj != null) break;
                }

                if (fireObj != null)
                {
                    if (fireplaceNewSprite != null)
                    {
                        var sr = fireObj.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sprite = fireplaceNewSprite;
                            Debug.Log($"TheEndManager: trocou sprite da lareira em '{fireObj.name}'.");
                        }
                        else
                        {
                            var img = fireObj.GetComponent<Image>();
                            if (img != null)
                            {
                                img.sprite = fireplaceNewSprite;
                                Debug.Log($"TheEndManager: trocou UI sprite da lareira em '{fireObj.name}'.");
                            }
                        }
                    }
                    // Disable Animator so runtime animation doesn't overwrite the new sprite
                    var animator = fireObj.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.enabled = false;
                        Debug.Log($"TheEndManager: desativou Animator da lareira em '{fireObj.name}'.");
                    }

                    // Disable Unity Light if present
                    var unityLight = fireObj.GetComponent<Light>();
                    if (unityLight != null)
                    {
                        unityLight.enabled = false;
                        Debug.Log($"TheEndManager: desativou Light em '{fireObj.name}'.");
                    }

                    // Disable Light2D (URP) if present
#if UNITY_2020_2_OR_NEWER
                    var l2 = fireObj.GetComponent<Light2D>();
                    if (l2 != null)
                    {
                        l2.intensity = 0f;
                        l2.enabled = false;
                        Debug.Log($"TheEndManager: desativou Light2D em '{fireObj.name}'.");
                    }
#endif
                }
                else
                {
                    Debug.LogWarning($"TheEndManager: não encontrou objeto da lareira '{fireplaceObjectName}' em cena '{scene.name}'.");
                }
            }

            // Ambient emitters: prefer assigned GameObject references (like LightManager), fallback to name-based search
            if (ambientToDisable != null && ambientToDisable.Length > 0)
            {
                foreach (var ambRef in ambientToDisable)
                {
                    if (ambRef == null) continue;

                    // Try to stop any registered loop on the referenced object's AudioSource first
                    var srcRef = ambRef.GetComponent<AudioSource>();
                    if (srcRef != null && AudioManager.Instance != null)
                    {
                        AudioManager.Instance.StopLoopOnSource(srcRef);
                        Debug.Log($"TheEndManager: parou loop em AudioSource de '{ambRef.name}' antes de desativar.");
                    }

                    // Disable the referenced GameObject directly (works even if reference points to another scene)
                    ambRef.SetActive(false);
                    Debug.Log($"TheEndManager: desativou ambient ref '{ambRef.name}' (direct) in scene '{scene.name}'.");

                    // Also look for a matching object inside this scene (fallback) and ensure its AudioSource stopped
                    var ambFallback = FindInSceneByName(roots, ambRef.name);
                    if (ambFallback != null && ambFallback != ambRef)
                    {
                        var src = ambFallback.GetComponent<AudioSource>();
                        if (src != null && AudioManager.Instance != null)
                        {
                            AudioManager.Instance.StopLoopOnSource(src);
                            Debug.Log($"TheEndManager: parou loop em AudioSource (fallback) de '{ambFallback.name}'.");
                        }
                        ambFallback.SetActive(false);
                        Debug.Log($"TheEndManager: desativou ambient (fallback) '{ambFallback.name}' em cena '{scene.name}'.");
                    }
                }
            }

            // Enable ambient: prefer reference if provided
            if (ambientToEnableRef != null)
            {
                GameObject targetEnable = null;
                if (ambientToEnableRef.scene.IsValid() && ambientToEnableRef.scene.name == scene.name)
                {
                    targetEnable = ambientToEnableRef;
                }
                else
                {
                    targetEnable = FindInSceneByName(roots, ambientToEnableRef.name);
                }

                if (targetEnable != null)
                {
                    targetEnable.SetActive(true);
                    Debug.Log($"TheEndManager: ativou ambient ref '{targetEnable.name}' em cena '{scene.name}'.");

                    // If there is no AmbientEmitter script, ensure loop is started on any AudioSource present
                    var emitter = targetEnable.GetComponent<AmbientEmitter>();
                    var src = targetEnable.GetComponent<AudioSource>();
                    // If there's an AmbientEmitter, try to start its sound via PlayLoopOnSource using its configured name.
                    if (emitter != null && AudioManager.Instance != null)
                    {
                        if (!string.IsNullOrEmpty(emitter.soundName))
                        {
                            if (src != null)
                            {
                                AudioManager.Instance.PlayLoopOnSource(src, emitter.soundName, emitter.category, emitter.localVolumeScale, true);
                                Debug.Log($"TheEndManager: PlayLoopOnSource called for emitter '{targetEnable.name}' with sound '{emitter.soundName}'.");
                            }
                            else
                            {
                                // Fallback to global player if there's no local AudioSource
                                AudioManager.Instance.PlayLoopGlobal(emitter.soundName, emitter.category, emitter.localVolumeScale, ambientFadeDuration, ambientTargetVolume);
                                Debug.Log($"TheEndManager: PlayLoopGlobal called for emitter '{targetEnable.name}' with sound '{emitter.soundName}'.");
                            }
                        }
                        else
                        {
                            // No soundName configured; if there's an AudioSource clip, use it
                            if (src != null && src.clip != null)
                            {
                                AudioManager.Instance.PlayLoopOnSource(src, src.clip.name, emitter.category, emitter.localVolumeScale, true);
                                Debug.Log($"TheEndManager: PlayLoopOnSource fallback using AudioSource.clip for '{targetEnable.name}' ('{src.clip.name}').");
                            }
                        }
                    }
                    else if (emitter == null && src != null && AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayLoopOnSource(src, src.clip != null ? src.clip.name : "", AudioManager.Category.Ambience, 1f, true);
                        Debug.Log($"TheEndManager: iniciou loop manualmente no AudioSource de '{targetEnable.name}'.");
                    }
                }
                else
                {
                    Debug.LogWarning($"TheEndManager: ambient ref '{ambientToEnableRef.name}' não encontrado na cena '{scene.name}'.");
                }
            }
            else if (!string.IsNullOrEmpty(ambientObjectToEnable))
            {
                var ambOn = FindInSceneByName(roots, ambientObjectToEnable);
                if (ambOn != null)
                {
                    ambOn.SetActive(true);
                    Debug.Log($"TheEndManager: ativou ambient '{ambientObjectToEnable}' em cena '{scene.name}'.");
                    var emitter = ambOn.GetComponent<AmbientEmitter>();
                    var src = ambOn.GetComponent<AudioSource>();
                    if (emitter != null && AudioManager.Instance != null)
                    {
                        if (!string.IsNullOrEmpty(emitter.soundName))
                        {
                            if (src != null)
                            {
                                AudioManager.Instance.PlayLoopOnSource(src, emitter.soundName, emitter.category, emitter.localVolumeScale, true);
                                Debug.Log($"TheEndManager: PlayLoopOnSource called for emitter '{ambOn.name}' with sound '{emitter.soundName}'.");
                            }
                            else
                            {
                                AudioManager.Instance.PlayLoopGlobal(emitter.soundName, emitter.category, emitter.localVolumeScale, ambientFadeDuration, ambientTargetVolume);
                                Debug.Log($"TheEndManager: PlayLoopGlobal called for emitter '{ambOn.name}' with sound '{emitter.soundName}'.");
                            }
                        }
                        else if (src != null && src.clip != null)
                        {
                            AudioManager.Instance.PlayLoopOnSource(src, src.clip.name, emitter.category, emitter.localVolumeScale, true);
                            Debug.Log($"TheEndManager: PlayLoopOnSource fallback using AudioSource.clip for '{ambOn.name}' ('{src.clip.name}').");
                        }
                    }
                    else if (emitter == null && src != null && AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayLoopOnSource(src, src.clip != null ? src.clip.name : "", AudioManager.Category.Ambience, 1f, true);
                        Debug.Log($"TheEndManager: iniciou loop manualmente no AudioSource de '{ambOn.name}'.");
                    }
                }
                else
                {
                    Debug.LogWarning($"TheEndManager: não encontrou ambient to enable '{ambientObjectToEnable}' em cena '{scene.name}'.");
                }
            }
        }

        // Applied — clear pending for this scene
        pendingSceneActions = false;
    }

    private void TryApplyOrScheduleSceneActions()
    {
        if (string.IsNullOrEmpty(targetSceneName)) return;

        // Marca que as ações foram solicitadas/aplicadas globalmente
        sceneActionsCompleted = true;

        // Procura cena já carregada com o nome alvo
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            if (string.Equals(scene.name, targetSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                ApplySceneActions(scene);
                return;
            }
        }

        // Se não encontrou a cena carregada, marca como pendente para aplicar quando ela carregar
        pendingSceneActions = true;
        Debug.Log($"TheEndManager: ações para cena '{targetSceneName}' agendadas (serão aplicadas ao carregar a cena).");
    }

    private GameObject FindInSceneByName(GameObject[] roots, string name)
    {
        if (roots == null || string.IsNullOrEmpty(name)) return null;
        foreach (var root in roots)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t == null) continue;
                if (t.name == name) return t.gameObject;
            }
        }
        return null;
    }

    private IEnumerator PlayEndDialogues()
    {
        var im = InteractionManager.Instance;
        if (im == null) yield break;

        foreach (var line in endDialogues)
        {
            if (string.IsNullOrEmpty(line)) continue;

            im.ShowDialogue(line);

            if (endDialoguesAutoAdvance)
            {
                var imType = im.GetType();
                var isTypingField = imType.GetField("isTyping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (isTypingField != null)
                {
                    while (true)
                    {
                        var val = isTypingField.GetValue(im);
                        if (val is bool b && !b) break;
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSecondsRealtime(1f);
                }

                if (endDialoguesAutoAdvanceDelay > 0f) yield return new WaitForSecondsRealtime(endDialoguesAutoAdvanceDelay);
                im.HideDialogueBox();
            }
            else
            {
                while (UIInputBlocker.IsBlocked)
                {
                    yield return null;
                }
            }
        }
    }

    private IEnumerator ImmediatePostEndEffectsRoutine()
    {
        // This mirrors LightManager: directly swap ambient emitters by reference (disable one, enable the other).
        if (ambientToDisableSingle != null)
        {
            Debug.Log($"TheEndManager: desativando emitter '{ambientToDisableSingle.name}' (scene: {ambientToDisableSingle.scene.name})");
            ambientToDisableSingle.SetActive(false);
        }
        else
        {
            Debug.Log("TheEndManager: ambientToDisableSingle não configurado.");
        }

        if (ambientToEnableSingle != null)
        {
            Debug.Log($"TheEndManager: ativando emitter '{ambientToEnableSingle.name}' (scene: {ambientToEnableSingle.scene.name})");
            ambientToEnableSingle.SetActive(true);
        }
        else
        {
            Debug.Log("TheEndManager: ambientToEnableSingle não configurado.");
        }

        // After swapping emitters, try applying scene-specific actions (or schedule them)
        TryApplyOrScheduleSceneActions();

        yield break;
    }
}
