using UnityEngine;
using static Shrine;

public static class ProjectileModifierFactory
{
    public static ProjectileModifiers RandomForTier(ShrineTier tier)
    {
        ProjectileModifiers m = ProjectileModifiers.None;

        switch (tier)
        {
            case ShrineTier.Tier1:
                m.speedMultiplier =
                    UnityEngine.Random.Range(0.9f, 1.1f);
                break;

            case ShrineTier.Tier2:
                m.speedMultiplier =
                    UnityEngine.Random.Range(0.9f, 1.3f);
                m.wobbleStrength =
                    UnityEngine.Random.Range(2f, 6f);
                m.wobbleFrequency =
                    UnityEngine.Random.Range(4f, 8f);
                break;

            case ShrineTier.Tier3:
                m.speedMultiplier =
                    UnityEngine.Random.Range(1.1f, 1.5f);
                m.wobbleStrength =
                    UnityEngine.Random.Range(6f, 12f);
                m.wobbleFrequency =
                    UnityEngine.Random.Range(6f, 12f);
                m.explodeDelay =
                    UnityEngine.Random.Range(0.8f, 1.5f);
                break;
        }

        return m;
    }
}
