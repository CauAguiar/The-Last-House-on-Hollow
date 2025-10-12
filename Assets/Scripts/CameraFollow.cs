using UnityEngine;

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
    
    void Start()
    {
        // Encontra a instância única e persistente do jogador
        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
        }
        else
        {
            
            Debug.LogError("CameraFollow não conseguiu encontrar a instância do PlayerController! O Player existe na cena inicial?");
        }
    }

    private void FixedUpdate()
    {
        if (player == null)
        {
            return;
        }

        
        Vector3 targetPosition = new Vector3(player.position.x, player.position.y, transform.position.z);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, timeLerp);
        smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
        smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        transform.position = smoothedPosition;
    }
}