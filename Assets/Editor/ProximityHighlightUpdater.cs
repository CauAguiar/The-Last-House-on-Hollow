#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Utility menu to set the proximity highlight color across scenes and prefabs.
/// </summary>
public static class ProximityHighlightUpdater
{
    private static readonly Color targetColor = new Color(0.7019608f, 0f, 0.1490196f, 1f); // #B30026, opaque

    [MenuItem("Tools/Apply Proximity Highlight Color (#&p)")]
    public static void ApplyColorToOpenScenesAndPrefabs()
    {
        int changedCount = 0;

        // 1) Update components in all loaded scenes
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                // InteractableBase and CollectibleItem are MonoBehaviours in project
                var interactables = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var mb in interactables)
                {
                    if (mb == null) continue;
                    var t = mb.GetType();
                    var field = t.GetField("proximityHighlightColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null && field.FieldType == typeof(Color))
                    {
                        field.SetValue(mb, targetColor);
                        EditorUtility.SetDirty(mb);
                        changedCount++;
                    }
                }
            }
            // Mark scene dirty so user can save
            EditorSceneManager.MarkSceneDirty(scene);
        }

        // 2) Update all prefabs in project
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var root = PrefabUtility.LoadPrefabContents(path);
            bool modified = false;
            var mbs = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in mbs)
            {
                if (mb == null) continue;
                var t = mb.GetType();
                var field = t.GetField("proximityHighlightColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(Color))
                {
                    field.SetValue(mb, targetColor);
                    modified = true;
                    changedCount++;
                }
            }
            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log($"ProximityHighlightUpdater: Applied color to {changedCount} components. Remember to save your scenes.");
    }
}
#endif
