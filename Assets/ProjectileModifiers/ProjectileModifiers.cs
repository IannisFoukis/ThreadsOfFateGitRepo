using UnityEngine;

[System.Serializable]
public struct ProjectileModifiers
{
    // Speed
    public float speedMultiplier;
    public float speedVariance;

    // Wobble
    public bool wobble;
    public float wobbleStrength;
    public float wobbleFrequency;

    // Delayed explosion
    public bool delayedExplode;
    public float explodeDelay;

    public static ProjectileModifiers Default => new ProjectileModifiers
    {
        speedMultiplier = 1f,
        speedVariance = 0f,
        wobble = false,
        wobbleStrength = 0f,
        wobbleFrequency = 0f,
        delayedExplode = false,
        explodeDelay = 0f
    };
}
