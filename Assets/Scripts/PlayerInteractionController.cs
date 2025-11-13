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
        Collider2D[] colliders;
        // If the layer mask is empty (value == 0) treat it as all layers to avoid accidental misconfiguration
        if (interactableLayerMask == 0)
        {
            colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        }
        else
        {
            colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactableLayerMask);
        }
        var currentInteractables = colliders.Select(c => c.GetComponent<IInteractable>()).Where(i => i != null).ToList();

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
            Debug.Log($"PlayerInteraction: clique não atingiu nenhum colisor. worldPos={worldPosition} mouseScreen={mousePosition} interactableLayerMask={(int)interactableLayerMask}");
            return;
        }

        // Filter colliders that implement IInteractable
        List<Collider2D> interactableColliders = new List<Collider2D>();
        foreach (var col in hitColliders)
        {
            if (col == null) continue;
            if (col.GetComponent<IInteractable>() != null)
            {
                interactableColliders.Add(col);
            }
        }

        if (interactableColliders.Count == 0)
        {
            Debug.Log("PlayerInteraction: havia colliders no ponto mas nenhum implementa IInteractable.");
            return;
        }

        // Choose the topmost interactable by sortingOrder (if SpriteRenderer available), then by Z (descending)
        Collider2D chosen = interactableColliders.OrderByDescending(c =>
        {
            var sr = c.GetComponent<SpriteRenderer>() ?? c.GetComponentInParent<SpriteRenderer>();
            int order = sr != null ? sr.sortingOrder : 0;
            float z = c.transform.position.z;
            return (order * 1000) - (int)(z * 100); // composite key: prioritize order, then nearer z
        }).FirstOrDefault();

        if (chosen == null)
        {
            Debug.Log("PlayerInteraction: nenhum collider interativo selecionado.");
            return;
        }

        IInteractable interactableObject = chosen.GetComponent<IInteractable>();
        if (interactableObject == null)
        {
            Debug.Log("PlayerInteraction: collider selecionado não implementa IInteractable (improvável).");
            return;
        }

        if (nearbyInteractables.Contains(interactableObject))
        {
            interactableObject.Interact();
        }
        else
        {
            Debug.Log("PlayerInteraction: objeto clicado está fora do raio de interação.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}