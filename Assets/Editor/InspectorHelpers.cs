using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InspectorHelpers
{
    [MenuItem("Tools/Find Missing Scripts In Scene(s)")]
    public static void FindMissingScriptsInOpenScenes()
    {
        int totalMissing = 0;
        var scenes = new List<Scene>();
        for (int i = 0; i < SceneManager.sceneCount; i++) scenes.Add(SceneManager.GetSceneAt(i));

        foreach (var scene in scenes)
        {
            if (!scene.isLoaded) continue;
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var missing = FindMissingComponentsRecursively(root);
                totalMissing += missing;
            }
        }

        Debug.Log($"FindMissingScripts: Found {totalMissing} missing script reference(s) in open scenes.");
    }

    private static int FindMissingComponentsRecursively(GameObject go)
    {
        int count = 0;
        var components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                Debug.LogWarning($"Missing script on GameObject '{go.name}' (path: {GetGameObjectPath(go)})", go);
                count++;
            }
        }

        foreach (Transform child in go.transform)
            count += FindMissingComponentsRecursively(child.gameObject);

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

    [MenuItem("Tools/Clear Console")] 
    public static void ClearConsole()
    {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        clearMethod.Invoke(null, null);
    }
}
