using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

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

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
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
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero, Mathf.Infinity, interactableLayerMask);

        if (hit.collider != null)
        {
            IInteractable interactableObject = hit.collider.GetComponent<IInteractable>();

            // A NOVA CONDIÇÃO:
            // O objeto é interativo E está na lista de objetos próximos?
            if (interactableObject != null && nearbyInteractables.Contains(interactableObject))
            {
                // Se ambas as condições forem verdadeiras, a interação é permitida.
                interactableObject.Interact();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}