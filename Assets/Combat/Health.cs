using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth;
    bool invulnerable;
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
        if (invulnerable)
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
    public void ScaleMaxHealth(float multiplier)
    {
        maxHealth = Mathf.RoundToInt(maxHealth * multiplier);
        currentHealth = maxHealth;
    }

    void Die()
    {
        // Joker kill path
        if (TryGetComponent<Joker>(out _))
        {
            if (JokerManager.Instance != null)
                JokerManager.Instance.OnJokerKilled();
        }

        if (EliteSpawner.Instance != null &&
    EliteSpawner.Instance.EliteAlive)
        {
            EliteSpawner.Instance.OnEliteKilled();

            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                enemy.ForceAggro(4f);
            }

            Debug.Log("[ELITE] Elite killed → FORCED AGGRO");
        }


        gameObject.SetActive(false);
        Destroy(gameObject);
    }
    public void SetInvulnerable(bool value)
    {
        invulnerable = value;
    }

}
