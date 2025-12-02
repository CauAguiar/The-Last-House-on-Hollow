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
/// Gerencia operações em lights (Light2D) que precisam ser ativadas/desativadas globalmente
/// ou entre cenas. Detecta lights marcadas com `DisableOnSafeOpen` e permite desligá-las
/// com atraso e com fade de intensidade.
/// Coloque este objeto em um GameObject persistente (DontDestroyOnLoad) na cena bootstrap.
/// </summary>
public class LightManager : MonoBehaviour
{
    public static LightManager Instance { get; private set; }

    [Tooltip("Se verdadeiro, o LightManager tentará descobrir lights nas cenas carregadas automaticamente.")]
    public bool autoDiscoverOnSceneLoad = true;

    // lista de luzes monitoradas (apenas Light2D que são marcadas com DisableOnSafeOpen)
    private readonly List<Light2D> trackedLights = new List<Light2D>();
    // se verdadeiro, as luzes marcadas devem permanecer desligadas globalmente
    private bool lightsGloballyDisabled = false;

    [Header("Efeitos pós-desligamento")]
    [Tooltip("Se verdadeiro, quando as luzes forem desligadas o LightManager tocará um SFX e provocará tremor de câmera e trocará emitters de ambiente.")]
    public bool triggerPostDisableEffects = true;

    [Tooltip("Nome do SFX no SoundBank a tocar após as luzes serem desligadas (deixe vazio para nenhum som)")]
    public string postDisableSfxName = "";

    [Tooltip("Duração do tremor de câmera (segundos)")]
    public float cameraShakeDuration = 3.5f;

    [Tooltip("Magnitude do tremor (em unidades de posição)")]
    public float cameraShakeMagnitude = 0.35f;

    [Tooltip("Objeto de emissão/ambient que será desativado após o tremor (ex: emitter de daytime)")]
    public GameObject ambientToDisable;

    [Tooltip("Objeto de emissão/ambient que será ativado após o tremor (ex: emitter de nighttime)")]
    public GameObject ambientToEnable;

    [Header("Diálogos pós-desligamento")]
    [Tooltip("Lista de linhas de diálogo que serão mostradas quando as luzes forem desligadas.")]
    public System.Collections.Generic.List<string> postDisableDialogues = new System.Collections.Generic.List<string>();

    [Tooltip("Se verdadeiro, as linhas de diálogo avançam automaticamente após serem digitadas.")]
    public bool postDisableDialoguesAutoAdvance = true;

    [Tooltip("Tempo em segundos entre o fim da digitação e o avanço automático para a próxima linha (aplicável quando AutoAdvance=true)")]
    public float postDisableAutoAdvanceDelay = 0.7f;

    [Header("Ações em outras cenas")]
    [Tooltip("Nome da cena que receberá ações adicionais (ex: 'SalaoPrincipal')")]
    public string targetSceneName = "SalaoPrincipal";

    [Tooltip("Nome do GameObject Tapete na cena alvo que terá seu sprite trocado")]
    public string tapeteObjectName = "Tapete";

    [Tooltip("Sprite a ser aplicado ao Tapete na cena alvo")]
    public Sprite tapeteNewSprite;

    [Tooltip("Se preenchido, procura um GameObject com esse nome e ativa-o (ex: obj com DoorController)")]
    public string doorObjectNameToEnable = "";

    // Estado pendente para aplicar ações quando a cena alvo for carregada
    private bool pendingSceneActions = false;
    // Marca que as ações para a cena alvo já foram disparadas (por exemplo, após o cofre)
    // Quando true, ApplySceneActionsToScene será executado sempre que a cena alvo for carregada.
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
        if (autoDiscoverOnSceneLoad)
            SceneManager.sceneLoaded += OnSceneLoaded;

        // Discover currently loaded scenes
        DiscoverLightsInLoadedScenes();
    }

    private void OnDisable()
    {
        if (autoDiscoverOnSceneLoad)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DiscoverLightsInScene(scene);
        // Caso haja ações pendentes para aplicar em cenas (ex: SalaoPrincipal), aplique quando a cena carregar
        if ((pendingSceneActions || sceneActionsCompleted) && !string.IsNullOrEmpty(targetSceneName) && string.Equals(scene.name, targetSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            ApplySceneActionsToScene(scene);
        }
    }

    /// <summary>
    /// Varre todas as cenas carregadas e registra Light2D marcadas com `DisableOnSafeOpen`.
    /// </summary>
    public void DiscoverLightsInLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
                DiscoverLightsInScene(scene);
        }
    }

    private void DiscoverLightsInScene(Scene scene)
    {
        if (!scene.isLoaded) return;
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var markers = root.GetComponentsInChildren<DisableOnSafeOpen>(true);
            foreach (var m in markers)
            {
                if (m == null) continue;
                var l = m.GetComponentInChildren<Light2D>();
                if (l != null)
                    RegisterLight(l);
            }
        }
    }

    /// <summary>
    /// Registra uma luz para ser controlada.
    /// Chamado automaticamente se a luz estiver marcada com `DisableOnSafeOpen`.
    /// </summary>
    public void RegisterLight(Light2D light)
    {
        if (light == null) return;
        if (!trackedLights.Contains(light)) trackedLights.Add(light);
        // Se o estado global já está desligado, assegure que a luz recém-registrada fique desligada
        if (lightsGloballyDisabled)
        {
            light.intensity = 0f;
            light.enabled = false;
        }
    }

    /// <summary>
    /// Remove registro da luz (por exemplo quando a cena é descarregada / objeto destruído).
    /// </summary>
    public void UnregisterLight(Light2D light)
    {
        if (light == null) return;
        trackedLights.Remove(light);
    }

    /// <summary>
    /// Desliga todas as lights registradas após um atraso em segundos. Pode optar por dar fade.
    /// </summary>
    public void DisableTrackedLightsAfter(float delaySeconds, bool fade = true, float fadeDuration = 1f)
    {
        // Marque o estado global imediatamente para que novas luzes descobertas em loads subsequentes
        // já sejam desligadas. O coroutine cuida do fade das luzes atualmente registradas.
        lightsGloballyDisabled = true;
        StartCoroutine(DisableTrackedAfterRoutine(delaySeconds, fade, fadeDuration));
    }

    private IEnumerator DisableTrackedAfterRoutine(float delay, bool fade, float fadeDuration)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        if (fade)
        {
            // capture current intensities
            var snapshots = new List<(Light2D light, float originalIntensity)>();
            foreach (var l in trackedLights.ToArray())
            {
                if (l == null) continue;
                snapshots.Add((l, l.intensity));
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float inv = 1f - t;
                foreach (var s in snapshots)
                {
                    if (s.light == null) continue;
                    s.light.intensity = s.originalIntensity * inv;
                }
                yield return null;
            }

            // finalize: turn off
            foreach (var s in snapshots)
            {
                if (s.light == null) continue;
                s.light.intensity = 0f;
                s.light.enabled = false;
            }
        }
        else
        {
            foreach (var l in trackedLights.ToArray())
            {
                if (l == null) continue;
                l.enabled = false;
            }
        }

        // Optionally clear list or keep it for potential re-enable

        // Após desligar as luzes, dispare efeitos configurados (SFX + tremor + troca de emitters)
        if (triggerPostDisableEffects)
        {
            // Efeitos imediatos (SFX + tremor); troca de emitters acontece ao final do tremor.
            StartCoroutine(ImmediatePostDisableEffectsRoutine());
            // Diálogos (se houver) rodem em paralelo, não bloqueando os efeitos.
            if (postDisableDialogues != null && postDisableDialogues.Count > 0 && InteractionManager.Instance != null)
            {
                StartCoroutine(PlayPostDisableDialogues());
            }
        }
    }
    
    private IEnumerator ImmediatePostDisableEffectsRoutine()
    {
        // Toca SFX (se configurado)
        if (!string.IsNullOrEmpty(postDisableSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(postDisableSfxName);
        }

        // Tremor de câmera — aguarda enquanto treme, depois prossegue para trocar emitters
        if (cameraShakeDuration > 0f && Camera.main != null)
        {
            Transform camT = Camera.main.transform;
            Vector3 originalPos = camT.localPosition;
            float elapsed = 0f;
            while (elapsed < cameraShakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damper = 1f - Mathf.Clamp01(elapsed / cameraShakeDuration);
                float x = (Random.value * 2f - 1f) * cameraShakeMagnitude * damper;
                float y = (Random.value * 2f - 1f) * cameraShakeMagnitude * damper;
                camT.localPosition = originalPos + new Vector3(x, y, 0f);
                yield return null;
            }
            camT.localPosition = originalPos;
        }

        // Troca de emitters de ambiente (adiciona logs para depuração)
        if (ambientToDisable != null)
        {
            Debug.Log($"LightManager: desativando emitter '{ambientToDisable.name}' (scene: {ambientToDisable.scene.name})");
            ambientToDisable.SetActive(false);
        }
        else
        {
            Debug.Log("LightManager: ambientToDisable não configurado.");
        }
        if (ambientToEnable != null)
        {
            Debug.Log($"LightManager: ativando emitter '{ambientToEnable.name}' (scene: {ambientToEnable.scene.name})");
            ambientToEnable.SetActive(true);
        }
        else
        {
            Debug.Log("LightManager: ambientToEnable não configurado.");
        }

        // Tenta aplicar ações na cena alvo imediatamente; se a cena não estiver carregada, marca pendência
        TryApplyOrScheduleSceneActions();

        yield break;
    }

    private void TryApplyOrScheduleSceneActions()
    {
        if (string.IsNullOrEmpty(targetSceneName)) return;

        // Marca que as ações foram solicitadas/aplicadas globalmente — garante que
        // elas sejam re-aplicadas em futuros carregamentos da cena alvo.
        sceneActionsCompleted = true;

        // Procura cena já carregada com o nome alvo
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            if (string.Equals(scene.name, targetSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                ApplySceneActionsToScene(scene);
                return;
            }
        }

        // Se não encontrou a cena carregada, marca como pendente para aplicar quando ela carregar
        pendingSceneActions = true;
        Debug.Log($"LightManager: ações para cena '{targetSceneName}' agendadas (serão aplicadas ao carregar a cena).");
    }

    private void ApplySceneActionsToScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        Debug.Log($"LightManager: aplicando ações na cena '{scene.name}'");

        var roots = scene.GetRootGameObjects();
        // Procura Tapete pelo nome entre os filhos (incluindo inativos)
        GameObject tapete = null;
        foreach (var root in roots)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t == null) continue;
                if (t.name == tapeteObjectName)
                {
                    tapete = t.gameObject;
                    break;
                }
            }
            if (tapete != null) break;
        }

        if (tapete != null)
        {
            // Tenta aplicar SpriteRenderer ou UI Image
            var sr = tapete.GetComponent<SpriteRenderer>();
            if (sr != null && tapeteNewSprite != null)
            {
                sr.sprite = tapeteNewSprite;
                Debug.Log($"LightManager: Tapete sprite atualizado em '{tapete.name}'");
            }
            else
            {
                var img = tapete.GetComponent<Image>();
                if (img != null && tapeteNewSprite != null)
                {
                    img.sprite = tapeteNewSprite;
                    Debug.Log($"LightManager: Tapete UI sprite atualizado em '{tapete.name}'");
                }
                else
                {
                    Debug.Log($"LightManager: Tapete encontrado ('{tapete.name}') mas sem SpriteRenderer/Image ou sprite não configurado.");
                }
            }
        }
        else
        {
            Debug.Log($"LightManager: não encontrou GameObject '{tapeteObjectName}' na cena '{scene.name}'.");
        }

        // Habilita objeto door por nome ou procura DoorController
        GameObject doorObj = null;
        if (!string.IsNullOrEmpty(doorObjectNameToEnable))
        {
            foreach (var root in roots)
            {
                var transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t == null) continue;
                    if (t.name == doorObjectNameToEnable)
                    {
                        doorObj = t.gameObject;
                        break;
                    }
                }
                if (doorObj != null) break;
            }
        }

        if (doorObj == null)
        {
            // procura componentes DoorController
            foreach (var root in roots)
            {
                var controllers = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var c in controllers)
                {
                    if (c == null) continue;
                    var t = c.GetType();
                    if (t.Name == "DoorController")
                    {
                        doorObj = c.gameObject;
                        break;
                    }
                }
                if (doorObj != null) break;
            }
        }

        if (doorObj != null)
        {
            doorObj.SetActive(true);
            Debug.Log($"LightManager: ativou door object '{doorObj.name}' na cena '{scene.name}'.");
        }
        else
        {
            Debug.Log($"LightManager: não encontrou DoorController ou objeto '{doorObjectNameToEnable}' na cena '{scene.name}'.");
        }

        // Aplicada — limpa pendência
        pendingSceneActions = false;
    }

    private IEnumerator PlayPostDisableDialogues()
    {
        var im = InteractionManager.Instance;
        if (im == null) yield break;

        foreach (var line in postDisableDialogues)
        {
            if (string.IsNullOrEmpty(line)) continue;

            // Mostra diálogo
            im.ShowDialogue(line);

            if (postDisableDialoguesAutoAdvance)
            {
                // Espera até que a digitação termine. access private 'isTyping' via reflection
                var imType = im.GetType();
                var isTypingField = imType.GetField("isTyping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (isTypingField != null)
                {
                    // espera enquanto isTyping == true
                    while (true)
                    {
                        var val = isTypingField.GetValue(im);
                        if (val is bool b && !b) break;
                        yield return null;
                    }
                }
                else
                {
                    // fallback: aguarda 1 segundo para a digitação completar
                    yield return new WaitForSecondsRealtime(1f);
                }

                // espera o delay configurado e fecha a caixa automaticamente
                if (postDisableAutoAdvanceDelay > 0f) yield return new WaitForSecondsRealtime(postDisableAutoAdvanceDelay);
                im.HideDialogueBox();
            }
            else
            {
                // aguarda até o jogador fechar manualmente (UIInputBlocker remove o token 'Dialogue')
                while (UIInputBlocker.IsBlocked)
                {
                    yield return null;
                }
            }
        }
    }

    /// <summary>
    /// Reactiva as luzes que foram registradas (não faz fade), usado se quiser reverter.
    /// </summary>
    public void EnableTrackedLights()
    {
        foreach (var l in trackedLights.ToArray())
        {
            if (l == null) continue;
            l.enabled = true;
        }
    }
}
