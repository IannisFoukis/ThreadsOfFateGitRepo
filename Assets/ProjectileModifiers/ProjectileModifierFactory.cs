using UnityEngine;
using static Shrine;

public static class ProjectileModifierFactory
{
    public static ProjectileModifiers ForShrineTier(
        ShrineTier tier, int tension )
    {
        var mods = ProjectileModifiers.Default;

        // Speed escalation
        mods.speedMultiplier = 1f + tension * 0.05f;

        // Wobble (Tier 2+)
        if (tier >= ShrineTier.Tier2 && Random.value < 0.4f)
        {
            mods.wobble = true;
            mods.wobbleStrength = Random.Range(0.2f, 0.6f);
            mods.wobbleFrequency = Random.Range(3f, 7f);
        }

        // Delayed explode (Tier 3)
        if (tier == ShrineTier.Tier3 && Random.value < 0.2f)
        {
            mods.delayedExplode = true;
            mods.explodeDelay = Random.Range(0.5f, 1.2f);
        }

        return mods;
    }
}
