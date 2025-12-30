using UnityEditor;
using UnityEngine;

// Simple editor validation to help designers ensure enemy prefab has required components
public class EnemyPrefabValidator : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        // No-op; keep class in editor so it compiles into editor assemblies
    }

    [InitializeOnLoadMethod]
    static void RunValidation()
    {
        // Run only in editor, don't spam
        if (Application.isPlaying) return;

        var guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets" });
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            if (prefab.GetComponent<Enemy>() == null) continue;

            // Validate required components
            bool ok = true;
            if (prefab.GetComponent<EnemyStateController>() == null) ok = false;
            if (prefab.GetComponent<Rigidbody2D>() == null) ok = false;
            if (prefab.GetComponent<Health>() == null) ok = false;

            if (!ok)
            {
                Debug.LogWarning($"[PREFAB VALIDATOR] Enemy prefab '{path}' missing required components (EnemyStateController / Rigidbody2D / Health)");
            }
        }
    }
}
