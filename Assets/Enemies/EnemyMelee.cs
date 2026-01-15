using UnityEngine;

public class EnemyMelee : MonoBehaviour
{
    [SerializeField] float baseSpeed = 2f;
    float speedMultiplier = 1f;

    public void SetSpeedMultiplier(float value)
    {
        speedMultiplier = value;
    }

    public float GetSpeed()
    {
        return baseSpeed * speedMultiplier;
    }
}
