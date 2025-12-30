using UnityEngine;

public class EnemyBehaviorController : MonoBehaviour
{
    public EnemyBehaviorTier currentTier = EnemyBehaviorTier.Base;

    EnemyChase chase;
    EnemyRanged ranged;
    EnemyCharger charger;
    EnemyMelee melee;

    public float corruptionMoveSpeedBonus = 1f;
    public float corruptionAttackSpeedBonus = 1f;

    void Awake()
    {
        chase = GetComponent<EnemyChase>();
        ranged = GetComponent<EnemyRanged>();
        charger = GetComponent<EnemyCharger>();
        melee = GetComponent<EnemyMelee>();
        
        // Subscribe to game events to handle spawn and corruption
        GameEvents.OnEnemySpawned += OnEnemySpawned;
        GameEvents.OnCorruptionChanged += OnCorruptionChanged;
    }

    void OnDestroy()
    {
        GameEvents.OnEnemySpawned -= OnEnemySpawned;
        GameEvents.OnCorruptionChanged -= OnCorruptionChanged;
    }

    void OnEnemySpawned(UnityEngine.GameObject enemy)
    {
        if (enemy != gameObject) return;

        // Initialize behavior tier according to current tier (idempotent)
        ApplyTier(currentTier);
    }

    void OnCorruptionChanged(int level)
    {
        ApplyCorruption(level);
    }

    public void ApplyTier(EnemyBehaviorTier tier)
    {
        currentTier = tier;

        switch (tier)
        {
            case EnemyBehaviorTier.Base:
                ApplyBaseStats();
                break;

            case EnemyBehaviorTier.Aggressive:
                ApplyAggressiveStats();
                break;

            case EnemyBehaviorTier.Elite:
                ApplyEliteStats();
                break;
        }
    }

    // -----------------------------
    //  BASE TIER
    // -----------------------------
    void ApplyBaseStats()
    {
        if (chase) chase.speed = 2f;
        if (melee) melee.speed = 2f;
        if (ranged) ranged.speed = 1.5f;
        if (charger) charger.chargeForce = 8f;
    }

    // -----------------------------
    //  AGGRESSIVE TIER
    // -----------------------------
    void ApplyAggressiveStats()
    {
        if (chase) chase.speed = 3f;
        if (melee) melee.speed = 3f;
        if (ranged) ranged.speed = 2f;
        if (charger) charger.chargeForce = 10f;
    }

    // -----------------------------
    //  ELITE TIER
    // -----------------------------
    void ApplyEliteStats()
    {
        if (chase) chase.speed = 4f;
        if (melee) melee.speed = 4f;
        if (ranged) ranged.speed = 2.5f;
        if (charger) charger.chargeForce = 12f;

        // Elite ranged enemies fire faster
        if (ranged != null)
            ranged.MultiplyFireRate(1.25f);
    }

    // -----------------------------
    //  CORRUPTION EFFECTS
    // -----------------------------
    public void ApplyCorruption(int corruptionLevel)
    {
        if (corruptionLevel <= 0) return;

        corruptionMoveSpeedBonus = 1f;
        corruptionAttackSpeedBonus = 1f;

        if (corruptionLevel >= 1)
            corruptionMoveSpeedBonus = 1.1f;

        if (corruptionLevel >= 2)
            corruptionAttackSpeedBonus = 1.15f;

        if (corruptionLevel >= 3 && currentTier != EnemyBehaviorTier.Elite)
            ApplyTier(EnemyBehaviorTier.Elite);

        Debug.Log($"[CORRUPTION] Applied lvl={corruptionLevel}");
    }
}
