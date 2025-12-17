using UnityEngine;

public static class CorruptionEffects
{
    public static void Apply(GameObject enemy)
    {
        int level = RunCorruptionState.Instance.CorruptionLevel;
        if (level <= 0) return;

        if (!enemy.TryGetComponent<EnemyStatsComponent>(out var stats))
            return;

        stats.damage += level;
        stats.moveSpeed += 0.2f * level;
    }
}
