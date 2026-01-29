using UnityEngine;

public class Keeper : MonoBehaviour
{
    public void Offer()
    {
        var contract = RoomAccess.Current;

        // Phase F6 (read-only): Keeper is aware of the current room contract.
        if (contract != null)
        {
            Debug.Log($"[Keeper] RoomContract detected: {contract.contractName} | Role={contract.roomRole} | " +
                      $"Silence={contract.silencePhase} | PressureSpike={contract.pressureSpike} | ReduceAudio={contract.reduceAudio}");
        }
        else
        {
            Debug.LogWarning("[Keeper] No RoomContract found (RoomDirector missing or unassigned).");
        }

        Debug.Log("KEEPER OFFERED OPTIONS:");
        Debug.Log("[C] Cleanse  |  [L] Lock  |  [T] Twist");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            Apply(KeeperOption.Cleanse);

        if (Input.GetKeyDown(KeyCode.L))
            Apply(KeeperOption.Lock);

        if (Input.GetKeyDown(KeyCode.T))
            Apply(KeeperOption.Twist);
    }

    void Apply(KeeperOption option)
    {
        switch (option)
        {
            case KeeperOption.Cleanse:
                Cleanse();
                break;

            case KeeperOption.Lock:
                Lock();
                break;

            case KeeperOption.Twist:
                Twist();
                break;
        }

        // Phase F6: no RunMemory calls here yet (your RunMemory doesn't have RecordKeeperInfluence).
        // Keeper consequences remain handled by existing systems:
        GodDirector.Instance?.EvaluateRun();
    }

    void Cleanse()
    {
        Debug.Log("KEEPER: Cleanse");

        RunCorruptionState.Instance.Reduce(1);
        CombatModifiers.Reset();
    }

    void Lock()
    {
        Debug.Log("KEEPER: Lock");

        RunCorruptionState.Instance.LockCorruption();
    }

    void Twist()
    {
        Debug.Log("KEEPER: Twist");

        RunCorruptionState.Instance.Increase(1);
        CombatModifiers.GlobalEnemyLifesteal += 1;
    }
}
