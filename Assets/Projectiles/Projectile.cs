using UnityEngine;

public class Projectile : MonoBehaviour
{
    Rigidbody2D rb;

    Vector2 direction;
    float speed;
    int damage;
    ProjectileModifiers mods;

    [Header("Explosion")]
    [SerializeField] float explodeRadius = 1f;
    [SerializeField] float explodeForce = 2f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Optional wobble (Phase C tuning safe)
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

        float variance = Random.Range(
            1f - mods.speedVariance,
            1f + mods.speedVariance
        );

        speed = baseSpeed * mods.speedMultiplier * variance;

        rb.linearVelocity = direction * speed;

        CancelInvoke();

        // Normal lifetime disable
        Invoke(nameof(Disable), lifetime);

        // Optional delayed explosion
        if (mods.delayedExplode)
            Invoke(nameof(Explode), mods.explodeDelay);
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
        // Prevent double disable
        CancelInvoke(nameof(Disable));

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(transform.position, explodeRadius);

        foreach (var c in hits)
        {
            if (c == null) continue;

            // Damage
            if (c.TryGetComponent<Health>(out var h))
            {
                Vector2 hitDir =
                    (c.transform.position - transform.position).normalized;
                h.TakeDamage(damage, hitDir);
            }

            // Knockback
            if (c.attachedRigidbody != null)
            {
                Vector2 forceDir =
                    (c.transform.position - transform.position).normalized;

                c.attachedRigidbody.AddForce(
                    forceDir * explodeForce,
                    ForceMode2D.Impulse
                );
            }
        }

        // TODO: Explosion VFX / SFX hook
        Disable();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explodeRadius);
    }
}