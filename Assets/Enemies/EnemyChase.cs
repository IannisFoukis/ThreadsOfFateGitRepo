using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    public float speed = 2f;

    Transform player;
    Rigidbody2D rb;
    bool stunned = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void FixedUpdate()
    {
        if (!player || stunned) return;

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = dir * speed;
    }

    public void Stun(float duration)
    {
        if (!stunned)
            StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        stunned = true;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(duration);
        stunned = false;
    }
}
