using UnityEngine;

public class PooledProjectile : MonoBehaviour
{
    Rigidbody2D rb;
    float lifetime;
    float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Fire(Vector2 dir, float speed, float life)
    {
        lifetime = life;
        timer = 0f;
        rb.linearVelocity = dir.normalized * speed;
        gameObject.SetActive(true);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
            ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // damage logic here
            ReturnToPool();
        }
    }

    void ReturnToPool()
    {
        rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
        ProjectilePool.Instance.Return(this);
    }
}
