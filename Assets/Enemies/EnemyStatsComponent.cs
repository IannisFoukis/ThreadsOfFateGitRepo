using UnityEngine;

public class EnemyStatsComponent : MonoBehaviour
{
    public EnemyStats baseStats;

    public int maxHealth;
    public int damage;
    public float moveSpeed;

    void Awake()
    {
        if (baseStats == null)
        {
            Debug.LogError("EnemyStatsComponent missing baseStats", this);
            return;
        }

        maxHealth = baseStats.maxHealth;
        damage = baseStats.damage;
        moveSpeed = baseStats.moveSpeed;
    }
}
