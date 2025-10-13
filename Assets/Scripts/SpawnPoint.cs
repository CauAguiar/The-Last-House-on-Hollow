using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("ID único do spawn")]
    public string spawnID;

    private void Awake()
    {
        // Se este spawn point tiver um ID...
        if (!string.IsNullOrEmpty(spawnID))
        {
            // ...ele verifica qual foi o último ID de spawn salvo.
            string targetID = PlayerPrefs.GetString("NextSpawnPointID");

            // Se o ID deste spawn point for o que estamos procurando...
            if (spawnID == targetID)
            {
                // ...ele encontra o jogador e o move para esta posição.
                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.transform.position = transform.position;

                    // Limpa a informação para que não seja usada novamente por engano.
                    PlayerPrefs.DeleteKey("NextSpawnPointID");
                }
            }
        }
    }
}

