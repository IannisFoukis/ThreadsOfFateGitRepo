using UnityEngine;

public class Projectile : MonoBehaviour
{
    Rigidbody2D rb;
    int damage;
    ProjectileModifiers mods;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Fire(
        Vector2 dir,
        float speed,
        float lifetime,
        int damage,
        ProjectileModifiers mods


    )
    {
        this.damage = damage;
        this.mods = mods;

        rb.linearVelocity = dir.normalized * speed;

        Invoke(nameof(Disable), lifetime);
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
}
