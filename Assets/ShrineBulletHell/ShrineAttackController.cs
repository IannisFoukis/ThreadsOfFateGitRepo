using System.Collections;
using UnityEngine;
using static Shrine;

public class ShrineAttackController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Shrine shrine;

    [Header("Projectile")]
    [SerializeField] float projectileSpeed = 6f;
    [SerializeField] float projectileLifetime = 1.5f;
    [SerializeField] int projectileDamage = 1;
    [Header("Config")]
    [SerializeField] ShrineProjectileConfig projectileConfig;
    // currently active tier config (set when attacks start)
    ShrineProjectileConfig.TierConfig currentTierConfig;

    [Header("Attack pacing")]
    [SerializeField] float tier1AttackInterval = 0.35f;
    [SerializeField] float tier2AttackInterval = 0.15f;
    [SerializeField] float tier3AttackInterval = 0.10f;
    [SerializeField] float tier3SkullStormInterval = 0.06f;

    [Header("Activator pulses")]
    [SerializeField] int pulseCount = 6;
    [SerializeField] float pulseSpeedMultiplier = 1.2f;

    [Header("State")]
    public ShrineTier currentTier;


    Coroutine activeRoutine;
    bool pendingEscalation;

    GameStateManager gsm;

    void Awake()
    {
        if (shrine == null) shrine = GetComponent<Shrine>();
        gsm = FindAnyObjectByType<GameStateManager>();
        if (projectileConfig == null)
        {
            projectileConfig = Resources.Load<ShrineProjectileConfig>("ShrineProjectileConfig");
            if (projectileConfig == null)
                Debug.Log("ShrineAttackController: No ShrineProjectileConfig found in Resources (optional).");
        }
    }

    public void StartAttacksForTier(ShrineTier tier)
    {
        var contract = RoomAccess.Current;
        if (contract == null || !contract.hasShrine)
        {
            Debug.Log("[ShrineAttackController] Shrine attacks disabled by contract");
            return;
        }

        // Keep local state in sync
        currentTier = tier;

        // Cache tier config for runtime use
        currentTierConfig = projectileConfig != null && shrine != null
            ? projectileConfig.Get(shrine.type, tier)
            : null;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        switch (tier)
        {
            case ShrineTier.Tier1:
                activeRoutine = StartCoroutine(RingRoutine());
                break;

            case ShrineTier.Tier2:
                activeRoutine = StartCoroutine(SpiralRoutine());
                break;

            case ShrineTier.Tier3:
                activeRoutine = StartCoroutine(ChaosRoutine());
                break;
        }
    }

    // ==========================
    // ACTIVATOR HOOKS (public API)
    // ==========================

    public void TriggerPulse()
    {
        // Called by Activator enemy on contact with shrine.
        FireRing(pulseCount, pulseSpeedMultiplier);
    }

    public void RequestEscalation()
    {
        // Next tick the running attack loop will restart for the current tier.
        pendingEscalation = true;
    }

    public void StartPatternPhase()
    {
        // Convenience: force a restart using the current tier.
        RequestEscalation();
    }

    // ==========================
    // PATTERNS
    // ==========================

    IEnumerator RingRoutine()
    {
        while (true)
        {
            if (pendingEscalation)
            {
                pendingEscalation = false;
                StartAttacksForTier(shrine.currentTier);
                yield break;
            }

            int count = 10;
            float speedMult = 1f;
            float interval = currentTierConfig != null ? currentTierConfig.attackInterval : tier1AttackInterval;

            FireRing(count, speedMult);
            yield return new WaitForSeconds(interval);
        }
    }

    IEnumerator SpiralRoutine()
    {
        float angle = 0f;

        while (true)
        {
            if (pendingEscalation)
            {
                pendingEscalation = false;
                StartAttacksForTier(shrine.currentTier);
                yield break;
            }

            // spiral “wall”
            for (int i = 0; i < 3; i++)
            {
                Vector2 dir = DirFromAngle(angle);
                FireProjectile(dir, 1f);
                angle += 25f;
            }

            float interval = currentTierConfig != null ? currentTierConfig.attackInterval : tier2AttackInterval;
            yield return new WaitForSeconds(interval);
        }
    }

    IEnumerator ChaosRoutine()
    {
        while (true)
        {
            if (pendingEscalation)
            {
                pendingEscalation = false;
                StartAttacksForTier(shrine.currentTier);
                yield break;
            }

            // phase A: rings
            for (int i = 0; i < 3; i++)
            {
                FireRing(12, 1f);
                float interval = currentTierConfig != null ? currentTierConfig.attackInterval : tier3AttackInterval;
                yield return new WaitForSeconds(interval);
            }

            yield return new WaitForSeconds(0.5f);

            // phase B: skull storm burst cadence
            for (int i = 0; i < 25; i++)
            {
                FireRing(6, 1.1f);
                float skullInterval = (currentTierConfig != null) ? Mathf.Max(0.02f, currentTierConfig.attackInterval * 0.6f) : tier3SkullStormInterval;
                yield return new WaitForSeconds(skullInterval);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    // ==========================
    // FIRING HELPERS
    // ==========================

    void FireRing(int count, float speedMult = 1f)
    {
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = DirFromAngle(i * step);
            FireProjectile(dir, speedMult);
        }
    }

    void FireProjectile(Vector2 dir, float speedMult = 1f, Vector3? overridePos = null)
    {
        // Choose tier config if present
        var cfg = currentTierConfig;

        float useSpeed = cfg != null ? cfg.baseSpeed * speedMult : projectileSpeed * speedMult;
        float useLifetime = cfg != null ? cfg.lifetime : projectileLifetime;
        int useDamage = cfg != null ? cfg.damage : projectileDamage;

        var mods = ProjectileModifierFactory.ForShrineTier(
            shrine != null ? shrine.currentTier : currentTier,
            (gsm != null && gsm.RunState != null) ? gsm.RunState.runTension : 0
        );

        // If tier config provides a prefab, try to get a pooled instance for that prefab
        if (cfg != null && cfg.projectilePrefab != null && ProjectilePool.Instance != null)
        {
            var pooledProj = ProjectilePool.Instance.Get(cfg.projectilePrefab);
            if (pooledProj != null)
            {
                pooledProj.transform.position = overridePos ?? transform.position;
                pooledProj.Fire(dir, useSpeed, useLifetime, useDamage, mods);
                return;
            }
        }

        // fallback to default pool
        if (ProjectilePool.Instance == null) return;

        var p = ProjectilePool.Instance.Get();
        if (p == null) return;

        p.transform.position = overridePos ?? transform.position;
        p.Fire(dir, useSpeed, useLifetime, useDamage, mods);
    }

    Vector2 DirFromAngle(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }
}
