using UnityEngine;

public class EnemyBehaviorController : MonoBehaviour
{
    public EnemyBehaviorTier currentTier = EnemyBehaviorTier.Base;

    EnemyChase chase;
    EnemyRanged ranged;
    EnemyCharger charger;

    public float corruptionMoveSpeedBonus = 1f;
    public float corruptionAttackSpeedBonus = 1f;


    void Awake()
    {
        chase = GetComponent<EnemyChase>();
        ranged = GetComponent<EnemyRanged>();
        charger = GetComponent<EnemyCharger>();
    }

    public void ApplyTier(EnemyBehaviorTier tier)
    {
        currentTier = tier;

        switch (tier)
        {
            case EnemyBehaviorTier.Base:
                EnableBase();
                break;

            case EnemyBehaviorTier.Aggressive:
                EnableAggressive();
                break;

            case EnemyBehaviorTier.Elite:
                EnableElite();
                break;

        }
        

    }

    void EnableBase()
    {
        if (charger) charger.enabled = false;
    }

    void EnableAggressive()
    {
        if (charger) charger.enabled = true;
    }

    void EnableElite()
    {
        if (charger) charger.enabled = true;

        if (ranged != null)
        {
            ranged.MultiplyFireRate(1.25f);
        }
    }
    public void ApplyCorruption(int corruptionLevel)
    {
        if (corruptionLevel <= 0) return;

        corruptionMoveSpeedBonus = 1f;
        corruptionAttackSpeedBonus = 1f;

        if (corruptionLevel >= 1)
            corruptionMoveSpeedBonus = 1.1f;

        if (corruptionLevel >= 2)
            corruptionAttackSpeedBonus = 1.15f;

        if (corruptionLevel >= 3 && currentTier != EnemyBehaviorTier.Elite)
            ApplyTier(EnemyBehaviorTier.Elite);

        Debug.Log($"[CORRUPTION] Applied lvl={corruptionLevel}");
    }

}
