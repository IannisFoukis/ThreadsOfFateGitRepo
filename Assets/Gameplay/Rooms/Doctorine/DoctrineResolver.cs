using UnityEngine;
using TOF.Rooms.Contracts;
using TOF.Core.Corruption;

public static class DoctrineResolver
{
    public static DoctrineState Resolve(RoomContract contract, CorruptionTier corruption)
    {
        var state = new DoctrineState();

        // ─────────────────────────────────────────────
        // BASELINE — readable, disciplined combat
        // ─────────────────────────────────────────────

        state.canRetreat = true;
        state.canSacrifice = false;

        state.formationDiscipline = 1f; // disciplined baseline
        state.fanatic = false;
        state.chaotic = false;

        state.aggressionMultiplier = 1f;
        state.coordinationDelay = 0f;

        // ─────────────────────────────────────────────
        // CORRUPTION MUTATION
        // ─────────────────────────────────────────────

        switch (corruption)
        {
            case CorruptionTier.None:
                // Clean, fair fight
                break;

            case CorruptionTier.Low:
                // Tension without loss of control
                state.aggressionMultiplier = 1.1f;
                break;

            case CorruptionTier.Medium:
                // Doctrine stress
                state.canRetreat = false;
                state.formationDiscipline = 0.85f;
                state.aggressionMultiplier = 1.2f;
                break;

            case CorruptionTier.High:
                // Doctrine fracture → CHAOTIC
                state.canRetreat = false;
                state.canSacrifice = true;

                state.formationDiscipline = 0.45f; // < 1 by rule
                state.chaotic = true;

                state.aggressionMultiplier = 1.35f;
                state.coordinationDelay = 0.15f;
                break;

            case CorruptionTier.Extreme:
                // Absolute conviction → FANATIC
                state.canRetreat = false;
                state.canSacrifice = true;

                state.formationDiscipline = 1.25f; // ≥ 1 by rule
                state.fanatic = true;

                state.aggressionMultiplier = 1.5f;
                state.coordinationDelay = -0.1f; // reacts faster
                break;
        }

        // ─────────────────────────────────────────────
        // CONTRACT MODIFIERS (KEEPER AUTHORITY)
        // ─────────────────────────────────────────────

        if (!contract.allowEncircle && state.formationDiscipline < 1f)
        {
            // Prevent broken encircle chaos if not allowed
            state.formationDiscipline = Mathf.Max(state.formationDiscipline, 0.6f);
        }

        if (contract.enforceHonestCombat)
        {
            // Keeper override: no sacrifice, no chaos
            state.canSacrifice = false;
            state.chaotic = false;
        }

        if (contract.pressureSpike)
        {
            state.aggressionMultiplier += 0.2f;
        }

        // ─────────────────────────────────────────────
        // HARD SANITY RULES (DO NOT REMOVE)
        // ─────────────────────────────────────────────

        // Fanatic and Chaotic are mutually exclusive
        if (state.fanatic)
        {
            state.chaotic = false;
            state.formationDiscipline = Mathf.Max(1f, state.formationDiscipline);
        }

        if (state.chaotic)
        {
            state.fanatic = false;
            state.formationDiscipline = Mathf.Min(state.formationDiscipline, 0.99f);
        }

        return state;
    }
}