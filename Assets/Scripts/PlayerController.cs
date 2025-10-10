using UnityEngine;

public class PlayerController: MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public void Awake()
    {
        // logica do singleton 
        if (Instance == null )
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
        }
    }
}
