using UnityEngine;

public class EnemyRanged : MonoBehaviour
{
    [SerializeField] float baseSpeed = 1.6f;
    [SerializeField] float baseFireRate = 1.2f;

    float speedMultiplier = 1f;
    float fireRateMultiplier = 1f;

    public void SetSpeedMultiplier(float value)
    {
        speedMultiplier = value;
    }

    public void MultiplyFireRate(float value)
    {
        fireRateMultiplier *= value;
    }

    public float GetSpeed()
    {
        return baseSpeed * speedMultiplier;
    }

    public float GetFireRate()
    {
        return baseFireRate * fireRateMultiplier;
    }
}
