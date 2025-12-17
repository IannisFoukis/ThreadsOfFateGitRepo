using UnityEngine;

public class GodDirector : MonoBehaviour
{
    public static GodDirector Instance;

    public GodType activeGod;
    public GodDemand currentDemand;
    [SerializeField] GodVisualFeedback visuals;
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
        Debug.Log("GodDirector.EvaluateRun CALLED");

        int corruption = RunCorruptionState.Instance.CorruptionLevel;

        if (corruption >= 3)
            activeGod = GodType.Chaos;
        else if (corruption == 0)
            activeGod = GodType.Order;
        else
            activeGod = GodType.Blood;
        CameraShake.Instance?.Shake(0.2f, 0.15f);

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
        visuals?.Show(activeGod);
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

        foreach (var enemy in FindObjectsByType<EnemyPunishVisual>(FindObjectsSortMode.None))
        {
            enemy.ApplyPunish(CombatModifiers.GlobalEnemyLifesteal);
        }

    }

}
