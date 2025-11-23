using System.Collections.Generic;

/// <summary>
/// Bloqueia interações com o mundo quando qualquer UI interativa está aberta.
/// Use Block("TokenDaUI") ao abrir e Unblock("TokenDaUI") ao fechar.
/// </summary>
public static class UIInputBlocker
{
    private static readonly HashSet<string> tokens = new HashSet<string>();
    // Enable this to get verbose logs about UI blocking operations
    private static bool verboseLogging = true;

    /// <summary>
    /// Verdadeiro quando há pelo menos um bloqueio ativo de UI.
    /// </summary>
    public static bool IsBlocked => tokens.Count > 0;

    /// <summary>
    /// Ativa o bloqueio com um token (nome estável da UI), ignorando duplicatas.
    /// </summary>
    public static void Block(string token)
    {
        if (string.IsNullOrEmpty(token)) token = "default";
        tokens.Add(token);
        if (verboseLogging)
        {
            // UIInputBlocker: Block called (log removed)
        }
    }

    /// <summary>
    /// Remove o bloqueio do token informado.
    /// </summary>
    public static void Unblock(string token)
    {
        if (string.IsNullOrEmpty(token)) token = "default";
        tokens.Remove(token);
        if (verboseLogging)
        {
            // UIInputBlocker: Unblock called (log removed)
        }
    }

    /// <summary>
    /// Remove todos os bloqueios ativos. Útil em troca de cena.
    /// </summary>
    public static void ClearAll()
    {
        tokens.Clear();
        if (verboseLogging)
        {
            // UIInputBlocker: ClearAll() called (log removed)
        }
    }
}
