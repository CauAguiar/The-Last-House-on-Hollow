using System.Collections.Generic;
using UnityEngine;
using System;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public event Action OnInventoryChanged;
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
            Debug.Log($"Item adicionado ao inventario: {item.itemName}");

            OnInventoryChanged?.Invoke();
        }
    }
    public List<InventoryItem> GetItems()
    {
        return items;
    }
}