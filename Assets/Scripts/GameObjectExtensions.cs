using UnityEngine;

public static class GameObjectExtensions
{
    /// <summary>
    /// Retorna o caminho completo do GameObject na hierarquia, exemplo: Root/Child/MyObject
    /// </summary>
    public static string GetPath(this GameObject go)
    {
        if (go == null) return "(null)";
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}
