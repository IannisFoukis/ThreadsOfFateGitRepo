using System;

[Serializable]
public class WorldModifiers
{
    // ───────── ENEMY BEHAVIOR ─────────
    public float enemyAggressionMultiplier = 1f;
    public float formationStrictness = 1f;
    public bool enemiesFavorHonestAttacks = false;

    // ───────── SILENCE ─────────
    public float silenceDurationMultiplier = 1f;

    // ───────── ENVIRONMENT ─────────
    public float environmentalHazardRate = 1f;

    // ───────── PLAYER AFFORDANCE ─────────
    public float playerGraceWindow = 1f;

    // ───────── WORLD TONE ─────────
    public bool worldFeelsWatchful = false;
}
