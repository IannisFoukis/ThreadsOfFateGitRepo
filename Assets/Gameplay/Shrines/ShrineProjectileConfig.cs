using UnityEngine;

[CreateAssetMenu(menuName = "Game/Shrine Projectile Config")]
public class ShrineProjectileConfig : ScriptableObject
{
    [System.Serializable]
    public class TierConfig
    {
        public string displayName;
        public GameObject projectilePrefab;
        public float baseSpeed = 6f;
        public float lifetime = 1.5f;
        public int damage = 1;
        public float attackInterval = 0.35f;
        public int pulseCount = 6;
        public float pulseSpeedMultiplier = 1.2f;
    }

    [System.Serializable]
    public class ShrineTypeEntry
    {
        public ShrineType type;
        public TierConfig[] tiers = new TierConfig[3];
    }

    public ShrineTypeEntry[] entries;

    public TierConfig Get(ShrineType type, Shrine.ShrineTier tier)
    {
        if (entries == null) return null;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].type == type)
            {
                int idx = (int)tier;
                if (entries[i].tiers == null) return null;
                if (idx < 0 || idx >= entries[i].tiers.Length) return null;
                return entries[i].tiers[idx];
            }
        }
        return null;
    }
}
