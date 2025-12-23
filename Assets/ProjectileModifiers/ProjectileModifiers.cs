using UnityEngine;
[System.Serializable]

public struct ProjectileModifiers
{
    public float speedMultiplier;
    public float wobbleStrength;
    public float wobbleFrequency;
    public float explodeDelay;

    public static ProjectileModifiers None => new ProjectileModifiers
    {
        speedMultiplier = 1f,
        wobbleStrength = 0f,
        wobbleFrequency = 0f,
        explodeDelay = -1f
    };
}
