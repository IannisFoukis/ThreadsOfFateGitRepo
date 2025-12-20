using UnityEngine;

public class EnemyBehaviorController : MonoBehaviour
{
    public EnemyBehaviorTier currentTier = EnemyBehaviorTier.Base;

    EnemyChase chase;
    EnemyRanged ranged;
    EnemyCharger charger;

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
}
