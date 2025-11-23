using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    public Button[] botoesDoMenu;
    public GameObject iconeAbobora;

    private int selecaoAtual = 0;
    private float posXAbobora;

    void Start()
    {
        if (iconeAbobora != null)
        {
            posXAbobora = iconeAbobora.transform.position.x;
        }

        AtualizarPosicaoAbobora();
    }

    public void DefinirSelecao(int index)
    {
        selecaoAtual = index;
        AtualizarPosicaoAbobora();
    }

    void AtualizarPosicaoAbobora()
    {
        if (iconeAbobora != null && botoesDoMenu.Length > 0 && selecaoAtual < botoesDoMenu.Length)
        {
            float posYBotao = botoesDoMenu[selecaoAtual].transform.position.y;

            iconeAbobora.transform.position = new Vector3(posXAbobora, posYBotao, iconeAbobora.transform.position.z);
        }
    }

    public void IniciarJogo()
    {
        SceneManager.LoadScene("StreetScene");
    }

    public void CarregarJogo()
    {
        // Carregar jogo (log removed)
    }

    public void Opcoes()
    {
        // Abrir opções (log removed)
    }

    public void SairJogo()
    {
        Application.Quit();
        // Saindo do jogo (log removed)
    }
}