using UnityEngine;
using UnityEngine.SceneManagement;

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



        currentHealth -= amount;

        // knockback / invincibility / FX already here

        // 🔹 BREAK TACTICAL SLOT ON HIT
        var slotLock = GetComponent<EnemySlotLock>();
        if (slotLock != null && slotLock.HasSlot)
        {
            slotLock.ReleaseSlot();
        }

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
        // If this is the player, end the run and restart at entry
        if (gameObject.CompareTag("Player") || TryGetComponent<PlayerController>(out _))
        {
            var gsm = FindAnyObjectByType<GameStateManager>();
            if (gsm != null)
            {
                gsm.EndRun(RunEndReason.PlayerDied);
            }
            else
            {
                Debug.LogWarning("Health: GameStateManager not found when player died — loading Entry scene directly.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Room_Entry");
            }

            // Do not destroy or deactivate the persistent player here — just trigger run end
            // GameStateManager will reset/position/reactivate the player when the Entry scene loads.
            return;
        }

        // Joker kill path
        if (TryGetComponent<Joker>(out _))
        {
            if (JokerManager.Instance != null)
                JokerManager.Instance.OnJokerKilled();
        }

        if (EliteSpawner.Instance != null && EliteSpawner.Instance.EliteAlive)
        {
            EliteSpawner.Instance.OnEliteKilled();

            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                var chase = e.GetComponent<EnemyChase>();
                if (chase != null)
                    chase.ForceAggro(1.5f);

            }

            Debug.Log("[ELITE] Elite killed → FORCED AGGRO");
        }

        // Unregister from TacticDirector if enemy
        if (TryGetComponent<Enemy>(out var enemy))
        {
            var tacticDirector = FindAnyObjectByType<TacticDirector>();
            if (tacticDirector != null)
                tacticDirector.Unregister(enemy);
        }

        gameObject.SetActive(false);
        Destroy(gameObject);

    }
    public void SetInvulnerable(bool value)
    {
        invulnerable = value;
    }

}
