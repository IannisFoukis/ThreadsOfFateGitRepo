using UnityEngine;

// ==================================================
// SCRIPT ROLE: AUTHORITY
// SYSTEM: Rooms / Narrative
// RESPONSIBILITY: Interpret run memory → translate world → conclude biome
// ==================================================

public class KeeperRoom : RoomController
{
    protected override void Start()
    {
        base.Start();
        Debug.Log("[KeeperRoom] STARTED");

        ShowKeeperReflection();
    }

    void ShowKeeperReflection()
    {
        if (RunContext.Instance == null || RunContext.Instance.memory == null)
        {
            Debug.LogError("[KeeperRoom] RunContext or RunMemory missing");
            return;
        }

        var memory = RunContext.Instance.memory;

        Debug.Log("[KEEPER] The Keeper observes your path...");
        Debug.Log($"[KEEPER] Shrines destroyed: {memory.shrineDestroyedCount}");
        Debug.Log($"[KEEPER] Shrines endured: {memory.shrineEnduredCount}");
        Debug.Log($"[KEEPER] Silence active during run: {memory.silenceActive}");

        // Reflection only — no translation yet
    }

    // Called by interaction / confirmation
    public void ConcludeKeeperRoom()
    {
        Debug.Log("[KEEPER] Judgment begins");

        ApplyKeeperTranslation();

        Debug.Log("[KEEPER] Judgment complete");
        CompleteRoom();
    }

    // ─────────────────────────────────────────────
    // CORE: MEMORY → WORLD TRANSLATION (Biome 1)
    // ─────────────────────────────────────────────
    private void ApplyKeeperTranslation()
    {
        var context = RunContext.Instance;
        var memory = context.memory;
        var world = context.worldModifiers;

        // Reset to neutral (important)
        world.enemyAggressionMultiplier = 1f;
        world.formationStrictness = 1f;
        world.enemiesFavorHonestAttacks = false;
        world.silenceDurationMultiplier = 1f;
        world.environmentalHazardRate = 1f;
        world.playerGraceWindow = 1f;
        world.worldFeelsWatchful = false;

        bool destroyedAny = memory.shrineDestroyedCount > 0;
        bool enduredAny = memory.shrineEnduredCount > 0;

        // ─────────────────────────────
        // EDICT OF SILENCE
        // ─────────────────────────────
        if (destroyedAny && !enduredAny)
        {
            world.enemiesFavorHonestAttacks = true;
            world.silenceDurationMultiplier = 1.25f;
            world.enemyAggressionMultiplier = 0.95f;
            world.environmentalHazardRate = 1.1f;

            Debug.Log("[KEEPER] Applied: Edict of Silence");
        }
        // ─────────────────────────────
        // MARK OF ENDURANCE
        // ─────────────────────────────
        else if (enduredAny && !destroyedAny)
        {
            world.enemyAggressionMultiplier = 1.1f;
            world.formationStrictness = 1.2f;
            world.silenceDurationMultiplier = 0.75f;
            world.worldFeelsWatchful = true;

            Debug.Log("[KEEPER] Applied: Mark of Endurance");
        }
        // ─────────────────────────────
        // FRACTURED WATCH
        // ─────────────────────────────
        else if (destroyedAny && enduredAny)
        {
            world.enemiesFavorHonestAttacks = true;
            world.formationStrictness = 1.1f;
            world.environmentalHazardRate = 1.1f;

            Debug.Log("[KEEPER] Applied: Fractured Watch");
        }
        else
        {
            Debug.Log("[KEEPER] No shrine interaction — world remains neutral");
        }
    }
}
