/// <summary>
/// Define um "contrato" para todos os objetos no jogo com os quais o jogador pode interagir.
/// Qualquer script que implementa esta interface é obrigado a ter um método Interact().
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Este método é chamado pelo script do jogador quando a interação ocorre (ex: clique do mouse).
    /// Cada objeto (porta, item, puzzle) implementará sua própria lógica aqui.
    /// </summary>
    void Interact();
}
