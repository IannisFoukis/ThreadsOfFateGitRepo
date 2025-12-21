using UnityEngine;

public class EnemyDash : MonoBehaviour
{
    public float dashSpeed = 8f;
    public float cooldown = 3f;

    float timer;
    Rigidbody2D rb;
    Transform player;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Vector2 dir = ((Vector2)player.position - rb.position).normalized;
            rb.AddForce(dir * dashSpeed, ForceMode2D.Impulse);
            timer = cooldown;
        }
    }
}
