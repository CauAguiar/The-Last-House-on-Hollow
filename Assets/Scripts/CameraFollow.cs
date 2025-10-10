using UnityEngine;

/// <summary>
/// Controla a câmera para seguir o jogador, com suavização e limites de área.
/// Encontra dinamicamente o jogador persistente em cada cena.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // A referência agora é privada, pois será encontrada automaticamente
    private Transform player;

    public float timeLerp = 0.1f;

    [Header("Limites da Câmera")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    // Usamos Start() para procurar o jogador assim que a cena começa.
    // Ele é executado depois de todos os Awakes(), então temos certeza que o PlayerController.Instance já existe.
    void Start()
    {
        // Encontra a instância única e persistente do jogador
        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
        }
        else
        {
            // Este erro aparecerá no console se você esquecer de colocar o prefab do Player na cena inicial.
            Debug.LogError("CameraFollow não conseguiu encontrar a instância do PlayerController! O Player existe na cena inicial?");
        }
    }

    private void FixedUpdate()
    {
        // Se, por algum motivo, o jogador não foi encontrado, não executa o código de seguir.
        if (player == null)
        {
            return;
        }

        // O resto do seu código de seguir permanece exatamente o mesmo.
        Vector3 targetPosition = new Vector3(player.position.x, player.position.y, transform.position.z);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, timeLerp);
        smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
        smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        transform.position = smoothedPosition;
    }
}