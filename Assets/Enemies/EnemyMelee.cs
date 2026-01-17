using UnityEngine;

public class EnemyMelee : MonoBehaviour
{
    public float attackRange = 1.5f;
    public float attackCooldown = 1.2f;

    private float lastAttackTime;
    private EnemyAgent agent;

    void Start()
    {
        agent = GetComponent<EnemyAgent>();
    }

    void Update()
    {
        if (agent == null)
            return;

        // Only attack when in formation slot
        if (agent.IsAtSlot())
        {
            TryAttackPlayer();
        }
    }

    void TryAttackPlayer()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        // Basic attack placeholder
        Debug.Log($"{name} performs MELEE ATTACK");

        lastAttackTime = Time.time;
    }
}
