using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    private HashSet<string> collectedItems = new HashSet<string>();

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
    public void MarkAsCollected(string itemID)
    {
        if (!collectedItems.Contains(itemID))
        {
            collectedItems.Add(itemID);
        }
    }

    public bool IsCollected (string itemID)
    {
        return collectedItems.Contains(itemID);
    }
}