#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class EnemyConfigCreator
{
    static EnemyConfigCreator()
    {
        CreateIfMissing();
    }

    [MenuItem("Tools/Create Default EnemyConfig (Resources)")]
    public static void CreateIfMissing()
    {
        const string resourcesDir = "Assets/Resources";
        const string assetPath = resourcesDir + "/EnemyConfig.asset";

        if (File.Exists(assetPath))
        {
            Debug.Log("EnemyConfig.asset already exists at Assets/Resources/EnemyConfig.asset");
            return;
        }

        if (!Directory.Exists(resourcesDir))
            Directory.CreateDirectory(resourcesDir);

        var config = ScriptableObject.CreateInstance<EnemyConfig>();

        // Create sensible defaults for each role
        var roles = System.Enum.GetValues(typeof(EnemyRole));
        config.entries = new EnemyConfig.RoleEntry[roles.Length];

        for (int i = 0; i < roles.Length; i++)
        {
            var r = (EnemyRole)roles.GetValue(i);
            var e = new EnemyConfig.RoleEntry();
            e.role = r;
            e.defaultBehaviorTier = EnemyBehaviorTier.Base;
            e.damageMultiplier = 1f;

            switch (r)
            {
                case EnemyRole.Melee:
                    e.enableMelee = true;
                    e.enableChase = true;
                    e.enableAbility = true;
                    break;

                case EnemyRole.Elite:
                    e.enableRanged = true;
                    e.enableAbility = true;
                    break;

                case EnemyRole.Charger:
                    e.enableAbility = true;
                    e.enableChase = true;
                    break;

                case EnemyRole.Defender:
                    e.enableActivator = true;
                    e.enableChase = true;
                    break;

                case EnemyRole.Activator:
                    e.enableMelee = true;
                    e.enableChase = true;
                    break;

            }

            config.entries[i] = e;
        }

        AssetDatabase.CreateAsset(config, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created default EnemyConfig.asset at Assets/Resources/EnemyConfig.asset");
    }
}
#endif
