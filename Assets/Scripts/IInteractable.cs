/// <summary>
/// Define um "contrato" para todos os objetos no jogo com os quais o jogador pode interagir.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Chamado quando o jogador clica no objeto.
    /// </summary>
    void Interact();

    /// <summary>
    /// Chamado pelo jogador quando ele entra no raio de proximidade do objeto.
    /// </summary>
    void OnProximityEnter();

    /// <summary>
    /// Chamado pelo jogador quando ele sai do raio de proximidade do objeto.
    /// </summary>
    void OnProximityExit();
}

