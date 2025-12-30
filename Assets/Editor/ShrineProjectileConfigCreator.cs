#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class ShrineProjectileConfigCreator
{
    [MenuItem("Tools/Create Default ShrineProjectileConfig (Resources)")]
    public static void CreateIfMissing()
    {
        const string resourcesDir = "Assets/Resources";
        const string assetPath = resourcesDir + "/ShrineProjectileConfig.asset";

        if (File.Exists(assetPath))
        {
            Debug.Log("ShrineProjectileConfig.asset already exists at Assets/Resources/ShrineProjectileConfig.asset");
            return;
        }

        if (!Directory.Exists(resourcesDir))
            Directory.CreateDirectory(resourcesDir);

        var config = ScriptableObject.CreateInstance<ShrineProjectileConfig>();

        var types = System.Enum.GetValues(typeof(ShrineType));
        config.entries = new ShrineProjectileConfig.ShrineTypeEntry[types.Length];

        for (int i = 0; i < types.Length; i++)
        {
            var t = (ShrineType)types.GetValue(i);
            var entry = new ShrineProjectileConfig.ShrineTypeEntry();
            entry.type = t;
            entry.tiers = new ShrineProjectileConfig.TierConfig[3];

            for (int k = 0; k < 3; k++)
            {
                var tc = new ShrineProjectileConfig.TierConfig();
                tc.displayName = t + " - Tier " + (k + 1);
                // leave projectilePrefab null so designer assigns
                tc.baseSpeed = 6f + k * 1.5f;
                tc.lifetime = 1.5f + k * 0.3f;
                tc.damage = 1 + k;
                tc.attackInterval = 0.35f - k * 0.05f;
                tc.pulseCount = 6 + k * 2;
                tc.pulseSpeedMultiplier = 1f + k * 0.15f;
                entry.tiers[k] = tc;
            }

            config.entries[i] = entry;
        }

        AssetDatabase.CreateAsset(config, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created default ShrineProjectileConfig.asset at Assets/Resources/ShrineProjectileConfig.asset");
    }
}
#endif
