#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShrineAttackController))]
public class ShrineAttackControllerValidator : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ctrl = (ShrineAttackController)target;

        // Validate: if projectileConfig exists, check tiers for prefab; otherwise ensure ProjectilePool has default prefab
        bool hasAnyTierPrefab = false;
        if (ctrl != null)
        {
            var cfgField = typeof(ShrineAttackController).GetField("projectileConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cfg = cfgField?.GetValue(ctrl) as ShrineProjectileConfig;
            if (cfg != null && cfg.entries != null)
            {
                foreach (var e in cfg.entries)
                {
                    if (e != null && e.tiers != null)
                    {
                        foreach (var t in e.tiers)
                        {
                            if (t != null && t.projectilePrefab != null)
                                hasAnyTierPrefab = true;
                        }
                    }
                }
            }

            if (!hasAnyTierPrefab)
            {
                // check pool default
                var pool = Object.FindAnyObjectByType<ProjectilePool>();
                if (pool == null)
                {
                    EditorGUILayout.HelpBox("Warning: No tier projectile prefabs assigned and no ProjectilePool present. Shrine attacks may fail.", MessageType.Warning);
                }
                else
                {
                    // access serialized field projectilePrefab
                    var poolType = typeof(ProjectilePool);
                    var f = poolType.GetField("projectilePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var defaultPrefab = f?.GetValue(pool) as Projectile;
                    if (defaultPrefab == null)
                        EditorGUILayout.HelpBox("Warning: No tier projectile prefabs assigned and ProjectilePool has no default projectile prefab.", MessageType.Warning);
                }
            }
        }
    }
}
#endif
