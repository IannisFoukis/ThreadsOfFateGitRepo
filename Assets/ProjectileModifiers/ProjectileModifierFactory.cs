using UnityEngine;
using static Shrine;

public static class ProjectileModifierFactory
{
    public static ProjectileModifiers ForShrineTier(
        ShrineTier tier,
        int tension
    )
    {
        var mods = ProjectileModifiers.Default;

        // Base speed scaling
        mods.speedMultiplier =
            1f + tension * 0.05f;

        // Wobble chance
        if (tier >= ShrineTier.Tier2 &&
            UnityEngine.Random.value < 0.4f)
        {
            mods.wobble = true;
            mods.wobbleStrength =
                UnityEngine.Random.Range(0.2f, 0.6f);
            mods.wobbleFrequency =
                UnityEngine.Random.Range(3f, 7f);
        }

        // Delayed explosion (Tier 3 only)
        if (tier == ShrineTier.Tier3 &&
            UnityEngine.Random.value < 0.2f)
        {
            mods.delayedExplode = true;
            mods.explodeDelay =
                UnityEngine.Random.Range(0.5f, 1.2f);
        }

        return mods;
    }
}
