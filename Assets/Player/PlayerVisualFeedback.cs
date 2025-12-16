using UnityEngine;
using System.Collections;

public class PlayerVisualFeedback : MonoBehaviour
{
    SpriteRenderer sprite;
    Invincibility invincibility;

    void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        invincibility = GetComponent<Invincibility>();
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

    void Update()
    {
        if (invincibility != null && invincibility.IsInvincible)
        {
            float alpha = Mathf.PingPong(Time.time * 10f, 1f);
            sprite.color = new Color(1f, 1f, 1f, alpha);
        }
        else
        {
            sprite.color = Color.white;
        }
    }
}
