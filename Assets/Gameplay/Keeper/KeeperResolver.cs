using UnityEngine;

public static class KeeperResolver
{
    static bool choiceAppliedThisEncounter = false;

    // ===== ENTRY ROOM: PURE MOOD (Phase C) =====
    static readonly string[] EntryDeathLines =
    {
        "You return again.\nThe gate does not judge.",
        "The door opens.\nIt always does.",
        "Still breathing.\nThat is enough.",
        "You hesitate.\nThe world does not.",
        "Death has weight.\nYou carry it back."
    };

    // Called AFTER the player makes a Keeper choice
    public static void ApplyChoice(KeeperChoice choice)
    {
        if (choiceAppliedThisEncounter)
        {
            Debug.LogWarning("[KeeperResolver] Choice already applied this encounter");
            return;
        }

        choiceAppliedThisEncounter = true;

        Debug.Log($"[KeeperResolver] Applying Keeper choice: {choice}");

        if (RunContext.Instance == null)
        {
            Debug.LogError("[KeeperResolver] RunContext.Instance is NULL");
            return;
        }

        var rules = RunContext.Instance.rules;
        if (rules == null)
        {
            Debug.LogError("[KeeperResolver] RunRules is NULL");
            return;
        }

        switch (choice)
        {
            case KeeperChoice.BindSouls:
                rules.soulsAreVolatile = true;
                Debug.Log("[Keeper] Rule applied: soulsAreVolatile = TRUE");
                break;

            case KeeperChoice.EnforceOrder:
                rules.enemiesCoordinateMore = true;
                Debug.Log("[Keeper] Rule applied: enemiesCoordinateMore = TRUE");
                break;

            case KeeperChoice.AccelerateChaos:
                rules.roomsChainAggressively = true;
                Debug.Log("[Keeper] Rule applied: roomsChainAggressively = TRUE");
                break;
        }

        Debug.Log("[KeeperResolver] Choice applied successfully");
    }

    /// <summary>
    /// Phase C: Entry room reaction.
    /// Pure mood. No judgment. No progression.
    /// </summary>
    public static string GetEntryReaction()
    {

        // Phase D: Keeper may remain silent
        const float silenceChance = 0.25f; // 25%

        if (Random.value < silenceChance)
            return null; // Silence is intentional

        int index = Random.Range(0, EntryDeathLines.Length);
        return EntryDeathLines[index];
    }

    public static void ResetForNewRun()
    {
        choiceAppliedThisEncounter = false;
    }

    // =========================================================
    // FUTURE (LOCKED): Entry behavior–based Keeper reactions
    // Re-enable intentionally when Entry becomes meaningful.
    // =========================================================

    static string GetHesitationLine(int tier)
    {
        switch (tier)
        {
            case 1: return "You waited at the threshold.";
            case 2: return "Again, you hesitate.";
            case 3: return "Indecision has become your companion.";
        }
        return null;
    }

    static string GetRushLine(int tier)
    {
        switch (tier)
        {
            case 1: return "You stepped through without pause.";
            case 2: return "You rush again.";
            case 3: return "Momentum blinds as easily as it carries.";
        }
        return null;
    }
}
