using UnityEngine;

[System.Serializable]
public class RoomContract
{
    // ─────────────────────────────────────────────
    // IDENTITY
    // ─────────────────────────────────────────────

    public string contractName = "Unnamed Contract";
    public RoomRole roomRole;

    // ─────────────────────────────────────────────
    // COMBAT ENABLEMENT
    // ─────────────────────────────────────────────

    public bool enableCombat = true;
    public bool useCoordinator = true;
    public bool enforceHonestCombat = false;

    // ─────────────────────────────────────────────
    // ENEMY COMPOSITION
    // ─────────────────────────────────────────────

    public int offenders;
    public int defenders;
    public int rangers;
    public int activators;
    public int jokers;

    // ─────────────────────────────────────────────
    // ELITES
    // ─────────────────────────────────────────────

    public bool allowElites = true;
    [Range(0f, 1f)] public float eliteChance = 0.1f;

    // ─────────────────────────────────────────────
    // FORMATION / DOCTRINE FLAGS
    // ─────────────────────────────────────────────

    public bool allowHammer = true;
    public bool allowEncircle = true;

    [Header("Formation Tuning")]
    [Range(0f, 3f)] public float hammerAggression = 1f;
    [Range(0.2f, 3f)] public float encircleSpeedMultiplier = 1f;

    [Header("Tactical Variance")]
    [Range(0f, 1f)] public float fakeOutChance = 0f;
    [Range(0f, 1f)] public float delayedDashChance = 0f;

    // ─────────────────────────────────────────────
    // MOOD / PRESSURE
    // ─────────────────────────────────────────────

    public bool silencePhase = false;
    public bool pressureSpike = false;
    public bool reduceAudio = false;

    // ─────────────────────────────────────────────
    // SHRINES
    // ─────────────────────────────────────────────

    public bool hasShrine = true;
    public bool allowShrines = true;

    // ─────────────────────────────────────────────
    // G3 — APPLY KEEPER WORLD STATE
    // ─────────────────────────────────────────────

    public void ApplyKeeperWorldState()
    {
        if (KeeperWorldState.silenceEnforced)
            silencePhase = true;

        if (KeeperWorldState.honestCombat)
            enforceHonestCombat = true;

        if (KeeperWorldState.pressureBias > 0)
            pressureSpike = true;

        if (KeeperWorldState.shrineHatred > 0)
            allowShrines = false;
    }
}
