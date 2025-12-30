using UnityEngine;

public class Projectile : MonoBehaviour
{
    Rigidbody2D rb;

    Vector2 direction;
    float speed;
    int damage;
    ProjectileModifiers mods;
    [SerializeField] float explodeRadius = 1f;
    [SerializeField] float explodeForce = 2f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (mods.wobble)
        {
            float wobble =
                Mathf.Sin(Time.time * mods.wobbleFrequency) *
                mods.wobbleStrength;

            Vector2 perp = Vector2.Perpendicular(direction);
            rb.linearVelocity =
                (direction + perp * wobble).normalized *
                speed *
                mods.speedMultiplier;
        }
    }

    public void Fire(
        Vector2 dir,
        float baseSpeed,
        float lifetime,
        int damage,
        ProjectileModifiers mods
    )
    {
        this.direction = dir.normalized;
        this.damage = damage;
        this.mods = mods;

        float variance =
            UnityEngine.Random.Range(
                1f - mods.speedVariance,
                1f + mods.speedVariance
            );

        speed = baseSpeed * mods.speedMultiplier * variance;

        rb.linearVelocity = direction * speed;

        CancelInvoke();
        // Normal lifetime disable
        Invoke(nameof(Disable), lifetime);

        // If modifier requests delayed explosion, schedule it separately
        if (mods.delayedExplode)
        {
            // Schedule Explode which will handle return/disable
            Invoke(nameof(Explode), mods.explodeDelay);
        }
    }

    void Disable()
    {
        rb.linearVelocity = Vector2.zero;

        var pooled = GetComponent<PooledProjectile>();
        if (pooled != null)
            pooled.ReturnToPool();
        else
            gameObject.SetActive(false);
    }

    void Explode()
    {
        // Simple explode behaviour for testing: log and disable projectile early.
        Debug.Log($"[PROJECTILE] Exploded at {transform.position}");

        // Stop normal lifetime invoke to avoid double-disable
        CancelInvoke(nameof(Disable));

        // Spawn explosion damage area: damage nearby Health components
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explodeRadius);
        foreach (var c in hits)
        {
            if (c == null) continue;

            // Apply damage if target has Health
            if (c.TryGetComponent<Health>(out var h))
            {
                // compute direction from projectile to target
                Vector2 hitDir = (c.transform.position - transform.position).normalized;
                h.TakeDamage(damage, hitDir);
            }

            // Apply knockback if target has Rigidbody2D
            if (c.attachedRigidbody != null)
            {
                c.attachedRigidbody.AddForce((c.transform.position - transform.position).normalized * explodeForce, ForceMode2D.Impulse);
            }
        }

        // TODO: spawn explosion VFX here
        Disable();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explodeRadius);
    }
}
