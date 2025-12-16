using UnityEngine;
using System.Collections;

public class EnemyVisualFeedback : MonoBehaviour
{
    SpriteRenderer sprite;

    void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
    }

    public void OnHit()
    {
        StartCoroutine(HitFlash());
    }

    IEnumerator HitFlash()
    {
        sprite.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sprite.color = Color.white;
    }

    public void OnDeath()
    {
        StartCoroutine(DeathFade());
    }

    IEnumerator DeathFade()
    {
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            sprite.color = Color.Lerp(Color.white, Color.clear, t / 0.3f);
            yield return null;
        }
    }
}
