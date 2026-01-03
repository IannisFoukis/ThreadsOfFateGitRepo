using UnityEngine;

public class KeeperResolver
{
    public static void Resolve()
    {
        Debug.Log("[KeeperResolver] Resolve() called");

        // --- Safety checks ---
        if (RunContext.Instance == null)
        {
            Debug.LogError("[KeeperResolver] RunContext.Instance is NULL. Is RunContext present in the scene?");
            return;
        }

        var memory = RunContext.Instance.memory;
        var rules = RunContext.Instance.rules;

        if (memory == null)
        {
            Debug.LogError("[KeeperResolver] RunMemory is NULL");
            return;
        }

        if (rules == null)
        {
            Debug.LogError("[KeeperResolver] RunRules is NULL");
            return;
        }

        Debug.Log("[KeeperResolver] Reading RunMemory...");
        Debug.Log($"[KeeperResolver] breatherRestCount = {memory.breatherRestCount}");
        Debug.Log($"[KeeperResolver] jokerKilled = {memory.jokerKilled}");

        // --- Interpretations ---
        if (memory.breatherRestCount >= 2)
        {
            rules.enemiesCoordinateMore = true;
            Debug.Log("[KeeperResolver] Rule applied: enemiesCoordinateMore = TRUE");
        }

        if (memory.jokerKilled)
        {
            rules.soulsAreVolatile = true;
            Debug.Log("[KeeperResolver] Rule applied: soulsAreVolatile = TRUE");
        }

        Debug.Log("[KeeperResolver] Resolve() completed");
    }
}
