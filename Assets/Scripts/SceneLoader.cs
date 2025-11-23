using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Gerencia o carregamento de cenas com um efeito de fade in/out.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Tooltip("A imagem de UI preta que será usada para o fade.")]
    [SerializeField] private Image fadePanel;

    [Tooltip("A duração do efeito de fade em segundos.")]
    [SerializeField] private float fadeDuration = 1f;

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

    /// <summary>
    /// Inicia o processo de carregamento de uma nova cena com fade.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        // Fade Out (tela fica preta)
        yield return StartCoroutine(Fade(1f));

        // Carrega a cena
        SceneManager.LoadScene(sceneName);

        // Fade In (tela volta ao normal)
        yield return StartCoroutine(Fade(0f));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadePanel == null)
        {
            // O Painel de Fade não foi atribuído no SceneLoader. (log removed)
            yield break; // Interrompe a coroutine se o painel não existir
        }

        float startAlpha = fadePanel.color.a;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            fadePanel.color = new Color(0, 0, 0, newAlpha);
            yield return null; // Espera até o próximo frame
        }

        // Garante que a transparência final seja exatamente o valor alvo
        fadePanel.color = new Color(0, 0, 0, targetAlpha);
    }
}

    
