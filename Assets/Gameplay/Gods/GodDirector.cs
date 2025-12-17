using UnityEngine;

public class GodDirector : MonoBehaviour
{
    public static GodDirector Instance;

    public GodType activeGod;
    public GodDemand currentDemand;

    void Awake()
    {
        Debug.Log("GodDirector awake");

        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void EvaluateRun()
    {
        int corruption = RunCorruptionState.Instance.CorruptionLevel;

        if (corruption >= 3)
            activeGod = GodType.Chaos;
        else if (corruption == 0)
            activeGod = GodType.Order;
        else
            activeGod = GodType.Blood;

        AssignDemand();
    }

    void AssignDemand()
    {
        switch (activeGod)
        {
            case GodType.Blood:
                currentDemand = GodDemand.KillQuickly;
                break;

            case GodType.Order:
                currentDemand = GodDemand.TakeNoDamage;
                break;

            case GodType.Chaos:
                currentDemand = GodDemand.AcceptCorruption;
                break;

            case GodType.Silence:
                currentDemand = GodDemand.DoNothing;
                break;
        }

        Debug.Log($"GOD {activeGod} DEMANDS: {currentDemand}");
    }

    public void ResolveDemand(bool success)
    {
        if (success)
        {
            Debug.Log("God pleased");
            CombatModifiers.GlobalEnemyLifesteal = Mathf.Max(
                0,
                CombatModifiers.GlobalEnemyLifesteal - 1
            );
        }
        else
        {
            Debug.Log("God angered");
            CombatModifiers.GlobalEnemyLifesteal += 1;
        }
    }
}
