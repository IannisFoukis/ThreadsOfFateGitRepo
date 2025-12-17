using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth;

    Invincibility invincibility;
    Knockback knockback;
    EnemyVisualFeedback enemyFX;
    PlayerVisualFeedback playerFX;

    void Awake()
    {
        currentHealth = maxHealth;

        // Cache components once (NO allocations later)
        TryGetComponent(out invincibility);
        TryGetComponent(out knockback);
        TryGetComponent(out enemyFX);
        TryGetComponent(out playerFX);
    }

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (invincibility != null && invincibility.IsInvincible)
            return;

        currentHealth -= amount;

        if (knockback != null)
            knockback.Apply(hitDirection);

        if (invincibility != null)
            invincibility.Trigger();

        if (enemyFX != null)
            enemyFX.OnHit();

        if (playerFX != null)
            playerFX.OnHit();

        if (HitStop.Instance != null)
            HitStop.Instance.Stop(0.06f);

        if (CombatModifiers.GlobalEnemyLifesteal > 0 &&
    gameObject.CompareTag("Enemy"))
        {
            int heal = CombatModifiers.GlobalEnemyLifesteal;
            currentHealth += heal;
        }

        Object.FindAnyObjectByType<RoomDemandTracker>()?.OnPlayerHit();



        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        // Joker kill path
        if (TryGetComponent<Joker>(out _))
        {
            if (JokerManager.Instance != null)
                JokerManager.Instance.OnJokerKilled();
        }

        Destroy(gameObject);
    }
}
