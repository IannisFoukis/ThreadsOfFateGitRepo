using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRanged : MonoBehaviour
{
    public float keepDistance = 5f;
    public float speed = 1.5f;

    Rigidbody2D rb;
    Transform player;
    EnemyStateController state;

    [Header("Ranged Stats")]
    [SerializeField] float fireCooldown = 1.5f;
   // [SerializeField] float projectileSpeed = 6f;

    float fireTimer;

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
        if (GameLock.IsLocked) return;
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            Shoot();
            fireTimer = fireCooldown;
        }
        if (!player || state.CurrentState == EnemyState.Hit || state.CurrentState == EnemyState.Dead)
            return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist < keepDistance)
        {
            Vector2 dir = (transform.position - player.position).normalized;
            rb.linearVelocity = dir * speed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void MultiplyFireRate(float multiplier)
    {
        fireCooldown /= multiplier; // higher multiplier = faster firing
    }

    void Shoot()
    {
        // existing shooting logic
    }
}
