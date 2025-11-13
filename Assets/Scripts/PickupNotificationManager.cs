using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerencia notificações de coleta (toasts) quando um item é adicionado ao inventário.
/// - Attach this script to a UI object (e.g. a Canvas child) that will act as the container.
/// - Provide a prefab with a TextMeshProUGUI or UnityEngine.UI.Text inside and optionally a CanvasGroup.
/// - The container should ideally have a VerticalLayoutGroup to stack multiple notifications.
/// </summary>
public class PickupNotificationManager : MonoBehaviour
{
    public static PickupNotificationManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Prefab do painel de notificação. Deve conter um TextMeshProUGUI ou um Text para mostrar o nome do item.")]
    [SerializeField] private GameObject notificationPrefab;

    [Tooltip("Transform que receberá as notificações (normalmente um GameObject com VerticalLayoutGroup). Se vazio, usa esse GameObject.")]
    [SerializeField] private Transform container;

    [Header("Timing")]
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private float fadeDuration = 0.35f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (container == null) container = this.transform;
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded += OnItemAdded;
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded -= OnItemAdded;
        }
    }

    private void OnItemAdded(InventoryItem item)
    {
        if (item == null) return;
        StartCoroutine(SpawnNotificationRoutine(item.itemName));
    }

    private IEnumerator SpawnNotificationRoutine(string itemName)
    {
        if (notificationPrefab == null)
        {
            Debug.LogWarning("PickupNotificationManager: notificationPrefab não está atribuído.");
            yield break;
        }

        var go = Instantiate(notificationPrefab, container);
        // Garantia: ative o gameobject do prefab (caso esteja desativado no prefab)
        go.SetActive(true);
        // Coloca em primeiro para aparecer no topo (se usar VerticalLayoutGroup com top alignment)
        go.transform.SetAsFirstSibling();

        // Tenta achar componentes de texto
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = itemName;
        }
        else
        {
            var uiText = go.GetComponentInChildren<Text>();
            if (uiText != null) uiText.text = itemName;
        }

        // Garante que há um CanvasGroup para controlar alpha
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        // Se o prefab tiver imagens com alpha zerado ou desabilitadas, habilita e corrige alpha
        var images = go.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (!img.enabled)
            {
                img.enabled = true;
            }
            if (img.color.a < 0.01f)
            {
                Color c = img.color;
                img.color = new Color(c.r, c.g, c.b, 1f);
                Debug.Log("PickupNotificationManager: Ajustei alpha de Image filha para 1. Verifique prefab para comportamento desejado.");
            }
        }

        // Fade in
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;

        // Mantém o tempo de exibição
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        t = 0f;
        float start = cg.alpha;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }
        Destroy(go);
    }
}
