using UnityEngine;
using static Shrine;

public class EnemyAbilityController : MonoBehaviour
{
    [Header("Current")]
    public EnemyAbilityTier currentTier = EnemyAbilityTier.Base;

    [Header("Cooldowns")]
    public float baseCooldown = 2f;
    public float aggressiveCooldown = 1.4f;
    public float eliteCooldown = 0.9f;

    float cooldownTimer;
    Enemy enemy;

    [Header("Projectile Settings")]
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] float projectileSpeed = 8f;
    [SerializeField] float projectileLifetime = 3f;
    
    void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {

            ExecuteAbility();
            ResetCooldown();
        }
    }

    // 🔥 NEW — escalation entry point
    public void ApplyEscalation(
        EnemyRole role,
        ShrineTier shrineTier,
        int runTension,
        bool midFight
    )
    {
        EnemyAbilityTier target = EnemyAbilityTier.Base;

        if (shrineTier == ShrineTier.Tier3 || runTension >= 7)
            target = EnemyAbilityTier.Elite;
        else if (shrineTier == ShrineTier.Tier2 || runTension >= 4)
            target = EnemyAbilityTier.Aggressive;

        currentTier = target;

        if (midFight)
        {
            cooldownTimer *= 0.5f; // instant pressure spike
            Debug.Log($"[ABILITY] Mid-fight escalation → {currentTier}");
        }
        else
        {
            Debug.Log($"[ABILITY] Tier set → {currentTier}");
        }
    }

    void ExecuteAbility()
    {
       
        switch (currentTier)
        {
            case EnemyAbilityTier.Base:
                BasicAttack();
                break;

            case EnemyAbilityTier.Aggressive:
                BasicAttack();
                AggressiveBonus();
                break;

            case EnemyAbilityTier.Elite:
                BasicAttack();
                AggressiveBonus();
                EliteAbility();
                break;
        }
    }

    void ResetCooldown()
    {
        cooldownTimer = currentTier switch
        {
            EnemyAbilityTier.Aggressive => aggressiveCooldown,
            EnemyAbilityTier.Elite => eliteCooldown,
            _ => baseCooldown
        };
    }

    void BasicAttack()
    {
       // if (!enemy || enemy.Target == null) return;
// Vector2 dir = (enemy.Target.position - transform.position).normalized;
        Fire(GetFireDirection());
    }

    void AggressiveBonus()
    {
        Fire(Quaternion.Euler(0, 0, 25) * Vector2.right);
        Fire(Quaternion.Euler(0, 0, -25) * Vector2.right);
    }

    void EliteAbility()
    {
        Fire(Quaternion.Euler(0, 0, 15) * GetFireDirection());
        Fire(Quaternion.Euler(0, 0, -15) * GetFireDirection());
    }


    void Fire(Vector2 dir)
    {
        if (ProjectilePool.Instance == null)
        {
            Debug.LogError("NO PROJECTILE POOL FOUND");
            return;
        }
            


        var projectile = ProjectilePool.Instance.Get();
        

        projectile.transform.position = transform.position;
        projectile.Fire(dir, projectileSpeed, projectileLifetime, projectileDamage, ProjectileModifiers.Default);

        Debug.Log("[ABILITY] Fired projectile");
    }
    Vector2 GetFireDirection()
    {
        Transform player = PlayerLocator.Instance?.Player;
        if (player == null)
            return Vector2.right;

        Vector2 baseDir = (player.position - transform.position).normalized;

        float spread = currentTier switch
        {
            EnemyAbilityTier.Base => 0f,
            EnemyAbilityTier.Aggressive => 6f,
            EnemyAbilityTier.Elite => 12f,
            _ => 0f
        };

        float angle = Random.Range(-spread, spread);
        return Quaternion.Euler(0, 0, angle) * baseDir;
    }




}
