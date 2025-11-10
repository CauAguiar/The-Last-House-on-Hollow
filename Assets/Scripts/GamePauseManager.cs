using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controle centralizado de pausa baseado em tokens. Enquanto houver pelo menos um token ativo, Time.timeScale = 0.
/// </summary>
public static class GamePauseManager
{
    private static readonly HashSet<string> tokens = new HashSet<string>();
    private const float Paused = 0f;
    private const float Unpaused = 1f;

    public static bool IsPaused => tokens.Count > 0;

    public static void Pause(string token)
    {
        if (string.IsNullOrEmpty(token)) token = "default";
        if (tokens.Add(token))
        {
            Apply();
        }
    }

    public static void Unpause(string token)
    {
        if (string.IsNullOrEmpty(token)) token = "default";
        if (tokens.Remove(token))
        {
            Apply();
        }
    }

    public static void ClearAll()
    {
        tokens.Clear();
        Apply();
    }

    private static void Apply()
    {
        Time.timeScale = tokens.Count > 0 ? Paused : Unpaused;
    }
}
