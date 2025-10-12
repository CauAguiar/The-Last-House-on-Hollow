using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class DoorController : MonoBehaviour
{
    [Header("Cena a carregar")]
    [SerializeField] private string sceneToLoad;

    [Header("ID do Spawn Point na cena de destino")]
    [SerializeField] private string targetSpawnID;

    [Header("Ignorar colisão até se mover")]
    [SerializeField] private float ignoreDistance = 1.0f;

    private Collider2D doorCollider;
    private Collider2D playerCollider;
    private Vector3 playerSpawnPosition;
    private bool ignoreCollision = false;

    private void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
        if (doorCollider == null)
        {
            Debug.LogWarning("DoorController precisa de um Collider2D no mesmo GameObject.");
        }
        else
        {
            Debug.Log($"[DoorController] Collider encontrado: {doorCollider.name}");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[DoorController] Trigger Enter: {other.name}");

        if (other.CompareTag("Player") || (other.transform.parent != null && other.transform.parent.CompareTag("Player")))
        {
            playerCollider = other;
            playerSpawnPosition = playerCollider.transform.position;

            Debug.Log($"[DoorController] Jogador detectado: {playerCollider.name} na posição {playerSpawnPosition}");

            if (doorCollider != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, doorCollider, true);
                ignoreCollision = true;
                Debug.Log($"[DoorController] Colisão ignorada temporariamente entre jogador e porta");
            }

            // Registrar callback para mover o jogador quando a nova cena carregar
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log($"[DoorController] Carregando cena: {sceneToLoad}");
            SceneLoader.Instance.LoadScene(sceneToLoad);
        }
    }

private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    Debug.Log($"Cena carregada: {scene.name}");

    // Procura o jogador na nova cena
    GameObject player = GameObject.FindWithTag("Player");
    if (player == null)
    {
        Debug.LogWarning("Player não encontrado na nova cena!");
        return;
    }

    // Procura o spawn point pelo ID na nova cena
    SpawnPoint spawn = GameObject.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None)
                                 .FirstOrDefault(s => s.spawnID == targetSpawnID);

    if (spawn != null)
    {
        player.transform.position = spawn.transform.position;
        Debug.Log($"Jogador movido para spawn '{targetSpawnID}' na posição {spawn.transform.position}");
    }
    else
    {
        // Lista todos os spawn points da nova cena
        SpawnPoint[] allSpawns = GameObject.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        string spawnList = allSpawns.Length > 0
            ? string.Join(", ", allSpawns.Select(s => $"'{s.spawnID}'").ToArray())
            : "Nenhum spawn point encontrado";

        Debug.LogWarning($"SpawnPoint com ID '{targetSpawnID}' não encontrado na cena '{scene.name}'. Spawn points disponíveis: {spawnList}");
    }
}



    private void Update()
    {
        if (ignoreCollision && playerCollider != null)
        {
            float distanceMoved = Vector3.Distance(playerSpawnPosition, playerCollider.transform.position);
            Debug.Log($"[DoorController] Distância do spawn: {distanceMoved}");

            if (distanceMoved >= ignoreDistance)
            {
                Physics2D.IgnoreCollision(playerCollider, doorCollider, false);
                ignoreCollision = false;
                Debug.Log($"[DoorController] Colisão com a porta reativada após mover {distanceMoved} unidades");
            }
        }
    }
}
