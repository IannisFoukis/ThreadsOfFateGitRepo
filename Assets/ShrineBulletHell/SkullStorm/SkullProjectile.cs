using System.Collections;
using UnityEngine;
using static Shrine;

public class SkullProjectile : MonoBehaviour
{
    Rigidbody2D rb;
    SpriteRenderer sr;
    Collider2D col;

    bool armed;
    float explodeDelay;
    int damage;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }
   

    public void Drop(Vector2 position, float delay, int dmg)
    {
        transform.position = position;
        explodeDelay = delay;
        damage = dmg;

        armed = false;
        rb.linearVelocity = Vector2.zero;
        col.enabled = true;

        sr.color = Color.white;
        gameObject.SetActive(true);
    }

    public void Arm()
    {
        if (armed) return;
        armed = true;
        StartCoroutine(ExplodeRoutine());
    }

    IEnumerator ExplodeRoutine()
    {
        float t = 0f;

        while (t < explodeDelay)
        {
            t += Time.deltaTime;
            sr.color = Color.Lerp(Color.white, Color.red, t / explodeDelay);
            yield return null;
        }

        Explode();
    }

    void Explode()
    {
        // TODO: damage player via overlap
        CameraShake.Instance?.Shake(0.25f, 0.2f);

        gameObject.SetActive(false);
    }


}
