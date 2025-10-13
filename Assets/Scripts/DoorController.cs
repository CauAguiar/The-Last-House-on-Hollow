using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Configuração da Porta")]
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string targetSpawnID;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica se o objeto que entrou no trigger tem a tag "Player"
        if (other.CompareTag("Player"))
        {
            // Salva a informação de para onde o jogador deve ir usando PlayerPrefs.
            PlayerPrefs.SetString("NextSpawnPointID", targetSpawnID);
            PlayerPrefs.Save(); // Garante que a informação seja salva imediatamente.

            // Chama o SceneLoader para carregar a cena com o efeito de fade.
            SceneLoader.Instance.LoadScene(sceneToLoad);
        }
    }
}

