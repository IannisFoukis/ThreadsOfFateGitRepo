using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        Invincibility iframe = GetComponent<Invincibility>();
        if (iframe != null && iframe.IsInvincible)
            return;

        currentHealth -= amount;

        Knockback kb = GetComponent<Knockback>();
        if (kb != null)
            kb.Apply(hitDirection);

        if (iframe != null)
            iframe.Trigger();

        if (HitStop.Instance != null)
            HitStop.Instance.Stop(0.15f);

        if (currentHealth <= 0)
            Die();
    }



    void Die()
    {
        Destroy(gameObject);
    }
}
