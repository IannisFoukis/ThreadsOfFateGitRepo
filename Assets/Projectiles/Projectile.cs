using UnityEngine;

public class Projectile : PooledProjectile
{
    [SerializeField] int damage = 1;

    public void Fire(Vector2 dir, float speed, float lifetime, int dmg)
    {
        damage = dmg;
        base.Fire(dir.normalized, speed, lifetime);

       // Debug.Log($"[PROJECTILE] Fired dir={dir} speed={speed}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;

        var health = other.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage, rb.linearVelocity.normalized);
        }

        ReturnToPool();
    }
}
