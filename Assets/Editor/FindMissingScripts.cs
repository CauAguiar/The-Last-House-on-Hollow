using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public static class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts In Scene")] 
    public static void FindInCurrentScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogWarning("No scene loaded.");
            return;
        }

        int count = 0;
        var roots = scene.GetRootGameObjects();
        foreach (var go in roots)
            count += FindInGO(go);

        Debug.Log($"FindMissingScripts: finished. Total missing scripts: {count}");
    }

    [MenuItem("Tools/Find Missing Scripts In Prefabs (Assets)")] 
    public static void FindInPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int total = 0;
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            total += FindInGameObject(prefab, path);
        }
        Debug.Log($"FindMissingScripts (Prefabs): finished. Total missing scripts: {total}");
    }

    private static int FindInGO(GameObject go)
    {
        int count = 0;
        var components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                Debug.LogWarning($"Missing script in GameObject: '{GetGameObjectPath(go)}' (scene)");
                count++;
            }
        }

        // recurse children
        foreach (Transform child in go.transform)
            count += FindInGO(child.gameObject);

        return count;
    }

    private static int FindInGameObject(GameObject go, string assetPath)
    {
        int count = 0;
        var components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                Debug.LogWarning($"Missing script in Prefab: '{assetPath}' (path: {GetGameObjectPath(go)})");
                count++;
            }
        }

        foreach (Transform child in go.transform)
            count += FindInGameObject(child.gameObject, assetPath);

        return count;
    }

    private static string GetGameObjectPath(GameObject go)
    {
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
