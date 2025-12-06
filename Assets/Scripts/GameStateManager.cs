using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    private HashSet<string> collectedItems = new HashSet<string>();
    private HashSet<string> unlockedDoors = new HashSet<string>();

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

    // --- Door unlock persistence ---
    public void MarkDoorUnlocked(string doorID)
    {
        if (string.IsNullOrEmpty(doorID)) return;
        if (!unlockedDoors.Contains(doorID))
        {
            unlockedDoors.Add(doorID);
        }
    }

    public bool IsDoorUnlocked(string doorID)
    {
        if (string.IsNullOrEmpty(doorID)) return false;
        return unlockedDoors.Contains(doorID);
    }
}