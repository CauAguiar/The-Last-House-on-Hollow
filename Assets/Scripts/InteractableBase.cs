using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Configuração de Interação")]
    [Tooltip("Texto que aparece quando o jogador inspeciona este objeto.")]
    [TextArea(2, 5)]
    public string inspectionText;

    public void Interact()
    {
        InteractionManager.Instance.ShowContextMenu(this, Mouse.current.position.ReadValue());
    }

    public virtual void OnInspect()
    {
        if (!string.IsNullOrEmpty(inspectionText))
        {
            InteractionManager.Instance.ShowDialogue(inspectionText);
        }
    }

    public virtual void OnUseItem(InventoryItem item)
    {
        Debug.Log($"O item '{item.itemName}' não pode ser usado em '{this.gameObject.name}'.");
    }

    public virtual void OnProximityEnter() { }
    public virtual void OnProximityExit() { }
}
