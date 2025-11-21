using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

/// <summary>
/// Gerencia a interação do jogador, combinando detecção de proximidade e clique do mouse.
/// </summary>
public class PlayerInteractionController : MonoBehaviour
{
    [Header("Configurações de Interação")]
    [Tooltip("O raio ao redor do jogador para detectar objetos interativos.")]
    public float interactionRadius = 2f;
    [Tooltip("Camada para objetos interativos")]
    public LayerMask interactableLayerMask;
    [Tooltip("Margem adicional (em unidades de mundo) para procurar colliders além do 'interactionRadius'.")]
    public float proximityExtraMargin = 0.5f;

    private PlayerControls playerControls;
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();

    private void Awake()
    {
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        playerControls.Player.Enable();
        playerControls.Player.Interact.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
        playerControls.Player.Interact.performed -= OnInteractPerformed;
    }

    private void Update()
    {
        CheckForNearbyInteractables();
    }

    private void CheckForNearbyInteractables()
    {
        // Remove any destroyed or non-MonoBehaviour references from the cached list
        for (int i = nearbyInteractables.Count - 1; i >= 0; i--)
        {
            var ia = nearbyInteractables[i];
            var mb = ia as MonoBehaviour;
            // If the implementing MonoBehaviour was destroyed, the cast will be null (Unity's == null handling)
            if (mb == null)
            {
                nearbyInteractables.RemoveAt(i);
            }
        }

        // Use the same layer mask used for raycasts so proximity matches clickable objects
        // We query a slightly larger circle (interactionRadius + proximityExtraMargin) and then
        // filter colliders based on per-interactable hoverScale so visuals that are slightly
        // larger become interactable from a bit farther away.
        Collider2D[] colliders;
        float queryRadius = interactionRadius + proximityExtraMargin;
        // If the layer mask is empty (value == 0) treat it as all layers to avoid accidental misconfiguration
        if (interactableLayerMask == 0)
        {
            colliders = Physics2D.OverlapCircleAll(transform.position, queryRadius);
        }
        else
        {
            colliders = Physics2D.OverlapCircleAll(transform.position, queryRadius, interactableLayerMask);
        }

        // Filter and apply per-interactable hover-scale based proximity test.
        var currentInteractables = new System.Collections.Generic.List<IInteractable>();
        foreach (var c in colliders)
        {
            if (c == null) continue;
            IInteractable ia = c.GetComponent<IInteractable>() ?? c.GetComponentInParent<IInteractable>();
            if (ia == null) continue;

            // Compute closest point on collider to player and distance
            Vector2 playerPos = transform.position;
            Vector2 closest = c.ClosestPoint(playerPos);
            float dist = Vector2.Distance(playerPos, closest);

            // Default multiplier = 1 (no change). If the interactable is an InteractableBase, use its HoverScaleFactor
            float hoverMult = 1f;
            var mb = ia as InteractableBase;
            if (mb != null)
            {
                hoverMult = Mathf.Max(0.01f, mb.HoverScaleFactor);
            }

            // Consider the object 'nearby' if its closest-point distance is within the scaled interaction radius
            if (dist <= interactionRadius * hoverMult)
            {
                currentInteractables.Add(ia);
            }
        }

        // Only call exit on interactables that are still valid
        foreach (var interactable in nearbyInteractables.Except(currentInteractables).ToList())
        {
            var mb = interactable as MonoBehaviour;
            if (mb != null)
            {
                interactable.OnProximityExit();
            }
        }

        foreach (var interactable in currentInteractables.Except(nearbyInteractables).ToList())
        {
            interactable.OnProximityEnter();
        }

        nearbyInteractables = currentInteractables;
    }

    /// <summary>
    /// Chamado quando o jogador clica para interagir.
    /// Agora verifica se o objeto clicado está dentro do raio de proximidade.
    /// </summary>
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Bloqueio global de interação se qualquer UI estiver aberta
        if (UIInputBlocker.IsBlocked)
        {
            return; // Ignora interação com o mundo enquanto UI ativa
        }
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (Camera.main == null)
        {
            Debug.LogError("PlayerInteraction: Camera.main is null. Ensure there's a camera tagged 'MainCamera' in the scene.");
            return;
        }

        // ScreenToWorldPoint requires a proper Z distance from the camera. For 2D scenes the world plane is often z=0.
        float zDistance = -Camera.main.transform.position.z; // distance from camera to world z=0 plane
        Vector3 screenPoint = new Vector3(mousePosition.x, mousePosition.y, zDistance);
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPoint);

        // Use OverlapPointAll to gather all colliders under the click (so non-interactive colliders won't block)
        Collider2D[] hitColliders;
        if (interactableLayerMask == 0)
        {
            hitColliders = Physics2D.OverlapPointAll(worldPosition);
        }
        else
        {
            hitColliders = Physics2D.OverlapPointAll(worldPosition, interactableLayerMask);
        }

        if (hitColliders == null || hitColliders.Length == 0)
        {
            return;
        }

        // Filter colliders that implement IInteractable (allow the IInteractable to be on a parent)
        List<Collider2D> interactableColliders = new List<Collider2D>();
        foreach (var col in hitColliders)
        {
            if (col == null) continue;
            if (col.GetComponent<IInteractable>() != null || col.GetComponentInParent<IInteractable>() != null)
            {
                interactableColliders.Add(col);
            }
        }

        if (interactableColliders.Count == 0)
        {
            return;
        }

        // Choose the most visually-top interactable. Priority:
        // 1) Sorting layer value, 2) sortingOrder, 3) nearest to camera (z), 4) closest to click point.
        Collider2D chosen = interactableColliders
            .Select(c => new {
                col = c,
                sr = c.GetComponent<SpriteRenderer>() ?? c.GetComponentInParent<SpriteRenderer>(),
                z = c.transform.position.z,
                dist = Vector2.Distance(worldPosition, c.bounds.center)
            })
            .OrderByDescending(x => x.sr != null ? UnityEngine.SortingLayer.GetLayerValueFromID(x.sr.sortingLayerID) : 0)
            .ThenByDescending(x => x.sr != null ? x.sr.sortingOrder : 0)
            .ThenBy(x => x.z)
            .ThenBy(x => x.dist)
            .Select(x => x.col)
            .FirstOrDefault();

        if (chosen == null)
        {
            return;
        }

        // Try to get the IInteractable directly on the collider, otherwise search parents (common for setups where collider is on a child)
        IInteractable interactableObject = chosen.GetComponent<IInteractable>() ?? chosen.GetComponentInParent<IInteractable>();
        if (interactableObject == null) return;
        if (nearbyInteractables.Contains(interactableObject))
        {
            interactableObject.Interact();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}