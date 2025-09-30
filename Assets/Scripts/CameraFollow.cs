using UnityEngine;

/// <summary>
/// Controla a câmera para seguir o jogador, com suavização e limites de área.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float timeLerp = 0.1f;

    [Header("Limites da Câmera")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    // FixedUpdate é bom para seguir objetos movidos pela física.
    private void FixedUpdate()
    {
        if (player == null)
        {
            Debug.LogWarning("Referência do Player não definida na Câmera.");
            return;
        }

        // Pega a posição do jogador e mantém o Z da câmera para -10
        Vector3 targetPosition = new Vector3(player.position.x, player.position.y, transform.position.z);

        // Suaviza o movimento da câmera em direção ao alvo
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, timeLerp);

        // Aplica os limites X e Y na posição final
        smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
        smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);

        // Atualiza a posição da câmera
        transform.position = smoothedPosition;
    }
}
