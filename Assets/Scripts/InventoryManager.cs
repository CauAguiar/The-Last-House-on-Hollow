using System.Collections.Generic;
using UnityEngine;
using System;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public event Action OnInventoryChanged;
    // Evento específico quando um item é adicionado. Fornece o item recém-adicionado.
    public event Action<InventoryItem> OnItemAdded;

    private List<InventoryItem> items = new List<InventoryItem>();

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

    public void AddItem(InventoryItem item)
    {
        if (item != null)
        {
            items.Add(item);
            OnInventoryChanged?.Invoke();
            OnItemAdded?.Invoke(item);
        }
    }

    public void RemoveItem(InventoryItem item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            OnInventoryChanged?.Invoke();
        }
    }

    public bool HasItem(InventoryItem item)
    {
        return items.Contains(item);
    }

    public List<InventoryItem> GetItems()
    {
        return items;
    }
}
