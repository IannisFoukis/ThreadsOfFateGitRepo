using UnityEngine;

public class KeeperRoom : RoomController
{
    protected override void Start()
    {
        base.Start();
        Debug.Log("[KEEPER] The Keeper observes your path...");
    }

    public void ConcludeKeeperRoom()
    {
        Debug.Log("[KEEPER] Judgment begins");

        ApplyKeeperJudgment();

        Debug.Log("[KEEPER] Judgment complete");
        CompleteRoom();
    }

    private void ApplyKeeperJudgment()
    {
        // G3: Keeper writes symbolic world mutations
        // No dependency on RunContext internals

        // Example baseline edicts (safe defaults)
        KeeperWorldState.honestCombat = true;
        KeeperWorldState.pressureBias += 1;

        Debug.Log("[KEEPER] World state mutated:");
        Debug.Log($"  honestCombat = {KeeperWorldState.honestCombat}");
        Debug.Log($"  pressureBias = {KeeperWorldState.pressureBias}");
    }
}
