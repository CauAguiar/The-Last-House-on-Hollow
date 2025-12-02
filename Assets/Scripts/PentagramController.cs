using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla o puzzle do pentagrama: cinco pratos nas pontas recebem itens específicos.
/// Quando todos os pratos contêm o item correto dispara um efeito: tremor, flash branco e
/// habilita objetos na cena SalaoPrincipal (`Saida` e `SaidaLuz`).
/// </summary>
public class PentagramController : MonoBehaviour
{
    public static PentagramController Instance { get; private set; }

    [Header("Persistence")]
    [Tooltip("ID único deste pentagrama para salvar/restaurar estado via GameStateManager (deixe vazio para não persistir)")]
    public string uniqueId = "";

    [ContextMenu("Generate Unique ID")]
    private void GenerateGuid()
    {
        uniqueId = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Generate Unique ID");
        UnityEditor.EditorUtility.SetDirty(this);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    [Header("Config")]
    // Scene-target actions are now handled by TheEndManager (persistent). Configure
    // target scene/objects there instead of on the PentagramController.

    [Header("Plates")]
    [Tooltip("Itens esperados para cada prato na ordem (Boneca, Alianca, Diario, Coracao, Vela Acesa)")]
    public InventoryItem[] expectedItems;

    [Tooltip("GameObjects dos pratos (opcional, para atualizar visual quando aceito)")]
    public GameObject[] plateObjects;

    [Tooltip("Sprite a aplicar ao prato quando o item correto for colocado (opcional)")]
    public Sprite plateAcceptedSprite;

    [Header("Efeitos")]
    [Tooltip("Imagem full-screen usada para o flash branco. Se não atribuída, será criada em runtime.")]
    public Image flashImage;
    [Tooltip("Duração total do flash (segundos)")]
    public float flashDuration = 1.2f;
    [Tooltip("Intensidade do tremor da câmera durante o efeito")]
    public float cameraShakeIntensity = 0.6f;
    [Tooltip("Duração do tremor da câmera")]
    public float cameraShakeDuration = 1.0f;

    [Header("Audio")]
    [Tooltip("SFX a tocar quando o puzzle for concluído (opcional)")]
    public string solvedSfxName = "";
    [Tooltip("Volume relativo do SFX de conclusão (0..1)")]
    [Range(0f,1f)] public float solvedSfxVolume = 1f;
    public AudioManager.Category solvedSfxCategory = AudioManager.Category.SFX;

    // Estado
    private bool[] plateSatisfied;
    private bool solved = false;
    



    // Called by PlateController when the player uses an item on a plate
    public void TryPlaceItem(int plateIndex, InventoryItem item, PlateController plate)
    {
        if (solved) return;
        if (plateIndex < 0 || plateIndex >= expectedItems.Length) return;
        if (item == null) return;

        var expected = expectedItems[plateIndex];
        if (expected != null && item == expected)
        {
            // Accept
            plateSatisfied[plateIndex] = true;
            // Update plate visual if assigned
            if (plateObjects != null && plateIndex < plateObjects.Length && plateObjects[plateIndex] != null)
            {
                var sr = plateObjects[plateIndex].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // Preferir o sprite/ícone do item se disponível, senão usar o sprite de aceitação configurado
                    if (item != null && item.icon != null)
                    {
                        sr.sprite = item.icon;
                        UpdateColliderForSprite(plateObjects[plateIndex], sr.sprite);
                    }
                    else if (plateAcceptedSprite != null)
                    {
                        sr.sprite = plateAcceptedSprite;
                        UpdateColliderForSprite(plateObjects[plateIndex], sr.sprite);
                    }
                }
            }

            // Consume the item from inventory
            if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveItem(item);

            // Persist plate state if uniqueId provided
            if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null)
            {
                string key = uniqueId + "_plate_" + plateIndex + "_" + item.itemName;
                GameStateManager.Instance.MarkAsCollected(key);
            }

            // Optionally provide feedback
            if (!string.IsNullOrEmpty(item.name)) Debug.Log($"Pentagram: item placed on plate {plateIndex}: {item.name}");

            // Disable interactivity on the plate now that it has an accepted item
            if (plate != null)
            {
                plate.SetInteractable(false);
            }

            // Check solved
            if (AllPlatesSatisfied())
            {
                StartCoroutine(OnSolvedRoutine());
            }
        }
        else
        {
            // Wrong item: inform the plate (includes dialogue and SFX)
            plate.OnWrongItemUsed(item);
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (expectedItems == null) expectedItems = new InventoryItem[0];
        plateSatisfied = new bool[Mathf.Max(0, expectedItems.Length)];
    }

    private void Start()
    {
        if (flashImage == null)
        {
            CreateFlashImage();
        }
        if (plateObjects == null) plateObjects = new GameObject[expectedItems.Length];

        // Restore saved plate states if uniqueId set
        if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null && expectedItems != null)
        {
            for (int i = 0; i < expectedItems.Length; i++)
            {
                var expected = expectedItems[i];
                if (expected == null) continue;
                string key = uniqueId + "_plate_" + i + "_" + expected.itemName;
                if (GameStateManager.Instance.IsCollected(key))
                {
                    plateSatisfied[i] = true;
                    // Update visual to item's icon (fallback handled by existing code)
                    if (plateObjects != null && i < plateObjects.Length && plateObjects[i] != null)
                    {
                        var sr = plateObjects[i].GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            if (expected.icon != null) sr.sprite = expected.icon;
                            else if (plateAcceptedSprite != null) sr.sprite = plateAcceptedSprite;
                            // Ensure collider matches the restored sprite
                            UpdateColliderForSprite(plateObjects[i], sr.sprite);
                        }
                        // If the plate GameObject has a PlateController, disable it because it's satisfied
                        var pc = plateObjects[i].GetComponent<PlateController>();
                        if (pc != null) pc.SetInteractable(false);
                    }
                }
            }

            // If all plates already satisfied at load, consider solved and notify TheEndManager
            if (AllPlatesSatisfied())
            {
                solved = true;
                if (TheEndManager.Instance != null)
                {
                    TheEndManager.Instance.TriggerEnd(uniqueId);
                }
            }
        }
    }


    private bool AllPlatesSatisfied()
    {
        if (plateSatisfied == null || plateSatisfied.Length == 0) return false;
        foreach (var b in plateSatisfied) if (!b) return false;
        return true;
    }

    private IEnumerator OnSolvedRoutine()
    {
        solved = true;

        // Persist overall solved state
        if (!string.IsNullOrEmpty(uniqueId) && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.MarkAsCollected(uniqueId + "_Solved");
            Debug.Log($"PentagramController: puzzle marcado como resolvido. uniqueId='{uniqueId}'");
            // Notify TheEndManager (persistent manager) to apply or schedule end actions in target scene
            if (TheEndManager.Instance != null)
            {
                TheEndManager.Instance.TriggerEnd(uniqueId);
            }
            Debug.Log("PentagramController: cenas carregadas no momento da resolução:");
            for (int si = 0; si < SceneManager.sceneCount; si++) Debug.Log($"  [{si}] {SceneManager.GetSceneAt(si).name} (loaded={SceneManager.GetSceneAt(si).isLoaded})");
        }

        // Block input + pause
        UIInputBlocker.Block("PentagramSolved");
        GamePauseManager.Pause("PentagramSolved");

        // Play solved SFX
        if (!string.IsNullOrEmpty(solvedSfxName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(solvedSfxName, solvedSfxCategory, solvedSfxVolume);
        }

        // Camera shake (coroutine)
        StartCoroutine(CameraShakeRoutine(cameraShakeDuration, cameraShakeIntensity));

        // Flash white
        if (flashImage != null)
        {
            flashImage.gameObject.SetActive(true);
            float half = flashDuration * 0.5f;
            // fade in
            yield return StartCoroutine(FadeImageAlpha(flashImage, 0f, 1f, half * 0.5f));
            // hold briefly
            yield return new WaitForSecondsRealtime(half);
            // fade out
            yield return StartCoroutine(FadeImageAlpha(flashImage, 1f, 0f, half * 0.5f));
            flashImage.gameObject.SetActive(false);
        }

        // TheEndManager will apply or schedule scene-target actions (if present).

        // Unblock
        UIInputBlocker.Unblock("PentagramSolved");
        GamePauseManager.Unpause("PentagramSolved");

        yield break;
    }

    // Scene-target enabling moved to TheEndManager (persistent). PentagramController no longer
    // searches or enables objects in other scenes.

    private IEnumerator CameraShakeRoutine(float duration, float intensity)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;
        Vector3 original = mainCam.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            Vector2 rnd = Random.insideUnitCircle * intensity * damper;
            mainCam.transform.localPosition = original + new Vector3(rnd.x, rnd.y, 0f);
            yield return null;
        }
        mainCam.transform.localPosition = original;
    }

    /// <summary>
    /// Update or create a Collider2D on the given GameObject to match the provided sprite.
    /// - If a PolygonCollider2D exists and the sprite contains physics shapes, the collider paths are regenerated.
    /// - If a BoxCollider2D exists it will be resized to sprite.bounds.
    /// - If no collider exists, a BoxCollider2D is added and sized to the sprite.
    /// </summary>
    private void UpdateColliderForSprite(GameObject go, Sprite sprite)
    {
        if (go == null || sprite == null) return;

        var poly = go.GetComponent<PolygonCollider2D>();
        if (poly != null)
        {
            try
            {
                int shapeCount = sprite.GetPhysicsShapeCount();
                poly.pathCount = shapeCount;
                var shape = new System.Collections.Generic.List<Vector2>();
                for (int i = 0; i < shapeCount; i++)
                {
                    shape.Clear();
                    sprite.GetPhysicsShape(i, shape);
                    poly.SetPath(i, shape.ToArray());
                }
                return;
            }
            catch
            {
                // Fallback to resizing box if physics shape APIs unavailable
            }
        }

        var box = go.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            // sprite.bounds is in local space of the Sprite; use size and center
            box.size = sprite.bounds.size;
            box.offset = sprite.bounds.center;
            return;
        }

        // No collider: add a BoxCollider2D sized to the sprite
        var added = go.AddComponent<BoxCollider2D>();
        added.size = sprite.bounds.size;
        added.offset = sprite.bounds.center;
    }

    private IEnumerator FadeImageAlpha(Image img, float from, float to, float t)
    {
        if (img == null) yield break;
        float elapsed = 0f;
        Color c = img.color;
        while (elapsed < t)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / t);
            float a = Mathf.Lerp(from, to, p);
            img.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        img.color = new Color(c.r, c.g, c.b, to);
    }

    private void CreateFlashImage()
    {
        // Build a simple overlay Canvas with full-screen white Image
        var canvasGO = new GameObject("Pentagram_FlashCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imgGO = new GameObject("FlashImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        var rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        flashImage = img;
        flashImage.gameObject.SetActive(false);
    }
}
