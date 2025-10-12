using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("ID único do spawn")]
    public string spawnID;

    private void Awake()
    {
        // Pega o ID que a porta salvou
        string targetID = PlayerPrefs.GetString("NextSpawnPoint");

        // Se o ID deste spawn point for o que estamos procurando
        if (spawnID == targetID)
        {
            // Encontra o jogador e o move para esta posição
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = transform.position;
                // Limpa a informação para a próxima transição
                PlayerPrefs.DeleteKey("NextSpawnPoint");
            }
        }
    }
}
