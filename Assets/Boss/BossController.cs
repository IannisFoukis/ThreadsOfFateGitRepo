using UnityEngine;

// Simple BossController that reads run history to adjust parameters
public class BossController : MonoBehaviour
{
    public int baseHealth = 50;
    public int baseDamage = 5;

    int health;
    int damage;

    void Awake()
    {
        var gsm = FindAnyObjectByType<GameStateManager>();
        if (gsm == null)
        {
            health = baseHealth;
            damage = baseDamage;
            return;
        }

        // Example adjustments based on run metrics
        health = baseHealth + gsm.RunData.roomsCleared * 5 + gsm.RunData.corruption * 3;
        damage = baseDamage + (gsm.RunData.tension / 2);

        Debug.Log($"BossController: tuned health={health} damage={damage} based on run history");
    }

    public void ApplyTo(GameObject boss)
    {
        var stats = boss.GetComponent<EnemyStatsComponent>();
        if (stats != null)
        {
            stats.maxHealth = health;
            // attempt to set damage via EnemyStatsComponent if present
            stats.damage = damage;
        }
    }
}
