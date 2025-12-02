using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor utility to find missing MonoBehaviour scripts and null UnityEngine.Object references
/// across open scenes and project prefabs. Use from menu: Tools/Find Missing References.
/// </summary>
public static class FindMissingReferences
{
    [MenuItem("Tools/Find Missing Scripts and Null References (Project & Scenes)")]
    public static void FindMissing()
    {
        int missingCount = 0;
        Debug.Log("FindMissingReferences: scanning open scenes and project prefabs...");

        // Scan active scene root objects
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            missingCount += CheckGameObjectRecursive(root);
        }

        // Scan prefabs in project (Assets)
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            missingCount += CheckPrefab(go, path);
        }

        Debug.Log($"FindMissingReferences: scan completed. {missingCount} issues found.");
    }

    private static int CheckPrefab(GameObject prefab, string path)
    {
        int found = 0;
        var components = prefab.GetComponentsInChildren<Component>(true);
        foreach (var c in components)
        {
            if (c == null)
            {
                Debug.LogWarning($"Missing MonoBehaviour in prefab: {path} (one of its children has a missing script)");
                found++;
                continue;
            }

            // Check serialized object fields for null UnityEngine.Object references
            var so = new SerializedObject(c);
            var prop = so.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                    {
                        string msg = $"Null reference in prefab '{path}': Component {c.GetType().Name} on GameObject '{c.gameObject.name}' has a missing reference field '{prop.name}'";
                        Debug.LogWarning(msg, prefab);
                        found++;
                    }
                }
            }
        }
        return found;
    }

    private static int CheckGameObjectRecursive(GameObject go)
    {
        int found = 0;
        var components = go.GetComponents<Component>();
        foreach (var c in components)
        {
            if (c == null)
            {
                Debug.LogWarning($"Missing MonoBehaviour on GameObject: {GetGameObjectPath(go)}", go);
                found++;
                continue;
            }

            var so = new SerializedObject(c);
            var prop = so.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                    {
                        Debug.LogWarning($"Null reference: Component {c.GetType().Name} on '{GetGameObjectPath(go)}' has missing field '{prop.name}'", go);
                        found++;
                    }
                }
            }
        }

        // Recurse children
        for (int i = 0; i < go.transform.childCount; i++)
        {
            found += CheckGameObjectRecursive(go.transform.GetChild(i).gameObject);
        }
        return found;
    }

    private static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        var t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
