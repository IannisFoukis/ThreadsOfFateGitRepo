using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Knockback : MonoBehaviour
{
    public float force = 5f;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Apply(Vector2 direction)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);

        EnemyChase chase = GetComponent<EnemyChase>();
        if (chase != null)
            chase.Stun(0.15f);
    }

}
