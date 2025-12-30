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

    float fireTimer;

    [Header("Projectile")]
    [SerializeField] float projectileSpeed = 5f;
    [SerializeField] float projectileLifetime = 2f;
    [SerializeField] int projectileDamage = 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        state = GetComponent<EnemyStateController>();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void FixedUpdate()
    {
        if (GameLock.IsLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            Shoot();
            fireTimer = fireCooldown;
        }

        if (player == null || state.CurrentState == EnemyState.Hit || state.CurrentState == EnemyState.Dead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

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
        fireCooldown /= multiplier;
    }

    void Shoot()
    {
        if (ProjectilePool.Instance == null)
        {
            Debug.LogError("[RANGED] ProjectilePool.Instance is null — ensure a ProjectilePool exists in the scene");
            return;
        }

        if (player == null) return;

        var p = ProjectilePool.Instance.Get();
        if (p == null) return;

        Vector2 dir = (player.position - transform.position).normalized;

        p.transform.position = transform.position;
        p.Fire(dir, projectileSpeed, projectileLifetime, projectileDamage, ProjectileModifiers.Default);
    }
}
