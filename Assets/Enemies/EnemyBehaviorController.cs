using UnityEngine;

public class EnemyBehaviorController : MonoBehaviour
{
    EnemyChase chase;
    EnemyMelee melee;
    EnemyRanged ranged;

    void Awake()
    {
        TryGetComponent(out chase);
        TryGetComponent(out melee);
        TryGetComponent(out ranged);
    }

    public void ApplySpeedMultiplier(float value)
    {
        chase?.SetSpeedMultiplier(value);
        melee?.SetSpeedMultiplier(value);
        ranged?.SetSpeedMultiplier(value);
    }

    public void ResetSpeed()
    {
        chase?.ResetSpeed();
    }

    public void ApplyFireRateMultiplier(float value)
    {
        ranged?.MultiplyFireRate(value);
    }
}
