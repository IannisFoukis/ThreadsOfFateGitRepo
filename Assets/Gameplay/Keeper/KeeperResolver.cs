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
}
