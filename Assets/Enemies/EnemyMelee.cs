using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMelee : MonoBehaviour
{
    public float speed = 2f;

    Rigidbody2D rb;
    Transform player;
    EnemyStateController state;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        state = GetComponent<EnemyStateController>();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void FixedUpdate()
    {
        if (!player || state.CurrentState == EnemyState.Hit || state.CurrentState == EnemyState.Dead)
            return;

        state.SetState(EnemyState.Chasing);

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = dir * speed;
    }
}
