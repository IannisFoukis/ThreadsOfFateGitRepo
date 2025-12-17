using UnityEngine;

public class Keeper : MonoBehaviour
{
    public void Offer()
    {
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
