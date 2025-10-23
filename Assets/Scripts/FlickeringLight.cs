using UnityEngine;
// Adicione esta linha se estiver usando o pipeline 2D da Unity com Light2D
using UnityEngine.Rendering.Universal; 

/// <summary>
/// Cria um efeito de luz bruxuleante (flickering) para uma fonte de luz.
/// Funciona tanto com o componente Light (3D) quanto com Light2D (URP 2D).
/// </summary>
public class FlickeringLight : MonoBehaviour
{
    [Header("Componente de Luz")]
    [Tooltip("Arraste o componente Light ou Light2D para este campo.")]
    [SerializeField] private Light2D lightSource; // Use Light se for um projeto 3D

    [Header("Configurações do Efeito")]
    [Tooltip("A intensidade mínima que a luz atingirá.")]
    [SerializeField] private float minIntensity = 0.8f;

    [Tooltip("A intensidade máxima que a luz atingirá.")]
    [SerializeField] private float maxIntensity = 1.2f;

    [Tooltip("A velocidade da oscilação. Valores maiores criam um piscar mais rápido.")]
    [SerializeField] [Range(0.1f, 10f)] private float flickerSpeed = 1.5f;

    // Usamos um offset aleatório para que múltiplas luzes não pisquem em sincronia
    private float randomOffset;

    private void Awake()
    {
        // Se a fonte de luz não foi definida no Inspector, tenta pegar no próprio objeto.
        if (lightSource == null)
        {
            lightSource = GetComponent<Light2D>();
        }

        // Gera um valor aleatório para garantir que cada lareira/vela tenha um padrão único
        randomOffset = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        if (lightSource == null) return;

        // Calcula a oscilação usando Perlin Noise para um efeito suave e natural
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, randomOffset);

        // Mapeia o resultado do Perlin Noise (que vai de 0 a 1) para o nosso intervalo de intensidade
        lightSource.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}