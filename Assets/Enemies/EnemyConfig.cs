using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [System.Serializable]
    public class RoleEntry
    {
        public EnemyRole role;

        [Header("Component enables")]
        public bool enableMelee = false;
        public bool enableRanged = false;
        public bool enableCharger = false;
        public bool enableChase = false;
        public bool enableAbility = false;
        public bool enableActivator = false;

        [Header("Behavior")]
        public EnemyBehaviorTier defaultBehaviorTier = EnemyBehaviorTier.Base;

        [Header("Scaling")]
        public float damageMultiplier = 1f;
    }

    public RoleEntry[] entries;

    public RoleEntry GetEntry(EnemyRole role)
    {
        if (entries == null) return null;
        for (int i = 0; i < entries.Length; i++)
            if (entries[i].role == role) return entries[i];
        return null;
    }
}
