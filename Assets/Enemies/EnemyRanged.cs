using UnityEngine;

public class EnemyRanged : MonoBehaviour
{
    public float fireCooldown = 1.5f;

    private float lastFireTime;
    private EnemyAgent agent;

    void Start()
    {
        agent = GetComponent<EnemyAgent>();
    }

    void Update()
    {
        if (agent == null)
            return;

        // Fire only when positioned correctly
        if (agent.IsAtSlot())
        {
            TryFireAtPlayer();
        }
    }

    void TryFireAtPlayer()
    {
        if (Time.time < lastFireTime + fireCooldown)
            return;

        Debug.Log($"{name} fires a projectile!");

        lastFireTime = Time.time;
    }
}
