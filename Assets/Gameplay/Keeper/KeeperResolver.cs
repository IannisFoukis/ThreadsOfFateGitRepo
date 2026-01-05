using UnityEngine;



public static class KeeperResolver
{
    static bool choiceAppliedThisEncounter = false;


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

    public static string GetEntryReaction()
    {
        var mem = RunContext.Instance.memory;
        if (mem == null)
            return null;

        int tier = 1;

        if (mem.entryHesitated)
        {
            if (mem.hesitationCount >= 5) tier = 3;
            else if (mem.hesitationCount >= 2) tier = 2;

            return GetHesitationLine(tier);
        }

        if (mem.entryRushed)
        {
            if (mem.rushCount >= 5) tier = 3;
            else if (mem.rushCount >= 2) tier = 2;

            return GetRushLine(tier);
        }

        return null;
    }

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
