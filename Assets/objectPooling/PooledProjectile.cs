using UnityEngine;

public abstract class PooledProjectile : MonoBehaviour
{
    protected Rigidbody2D rb;
    private ProjectilePool pool;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void AssignPool(ProjectilePool poolRef)
    {
        pool = poolRef;
    }

    public virtual void Fire(Vector2 dir, float speed, float lifetime)
    {
        gameObject.SetActive(true);

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.WakeUp();

        rb.linearVelocity = dir * speed;

        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifetime);
    }

    protected void ReturnToPool()
    {
        CancelInvoke();
        rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);

        //pool?.Return(this);
    }
}
