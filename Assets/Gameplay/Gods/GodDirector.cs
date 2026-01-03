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

        if (RunCorruptionState.Instance == null)
        {
            Debug.LogError("[GodDirector] RunCorruptionState not found.");
            return;
        }

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

        foreach (var enemy in FindEnemyPunishVisuals())
        {
            if (enemy == null) continue;
            enemy.ApplyPunish(CombatModifiers.GlobalEnemyLifesteal);
        }

    }
    public void ModifyCorruption(int amount)
    {
        Debug.Log($"[GodDirector] Corruption modified by {amount}");

        if (RunCorruptionState.Instance == null)
        {
            Debug.LogError("[GodDirector] RunCorruptionState not found.");
            return;
        }

        // CorruptionLevel is read-only; use the state API to mutate.
        if (amount > 0)
            RunCorruptionState.Instance.Increase(amount);
        else if (amount < 0)
            RunCorruptionState.Instance.Reduce(-amount);

        // Optional but VERY fitting:
        EvaluateRun();
    }

    public void OnCorruptionAccepted(int amount)
    {
        ModifyCorruption(amount);
    }

    static EnemyPunishVisual[] FindEnemyPunishVisuals()
    {
        // Unity 2023+ prefers FindObjectsByType; older versions only have FindObjectsOfType.
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<EnemyPunishVisual>(FindObjectsSortMode.None);
#else
        // Avoid compile errors on older Unity versions.
        return Object.FindObjectsOfType<EnemyPunishVisual>();
#endif
    }


}
