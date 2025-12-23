using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Shrine;

public class ShrineAttackController : MonoBehaviour
{
    [Header("Combat Bounds")]
    [SerializeField] Collider2D combatBounds;

    [Header("Normal Projectiles")]
    [SerializeField] float projectileSpeed = 6f;
    [SerializeField] float projectileLifetime = 3f;
    [SerializeField] int projectileDamage = 1;

    [Header("Skull Storm")]
    [SerializeField] SkullProjectile skullPrefab;
    [SerializeField] int skullTier1 = 40;
    [SerializeField] int skullTier2 = 70;
    [SerializeField] int skullTier3 = 120;

    [SerializeField] float armDelay = 1.5f;
    [SerializeField] float armInterval = 0.5f;
    [SerializeField] int armedPerWave = 20;
    [SerializeField] int skullDamage = 1;

    [SerializeField] Shrine shrine;

    GameStateManager gameState;

    Coroutine attackRoutine;
    Coroutine skullRoutine;
    void Awake()
    {
        gameState = FindAnyObjectByType<GameStateManager>();
    }

    /* ===================== PUBLIC API ===================== */

    public void StartAttacksForTier(ShrineTier tier)
    {
        StopAllAttacks();

        // Bullet hell
        attackRoutine = tier switch
        {
            ShrineTier.Tier1 => StartCoroutine(RingRoutine(12, 1f)),
            ShrineTier.Tier2 => StartCoroutine(SpiralRoutine()),
            ShrineTier.Tier3 => StartCoroutine(ChaosRoutine()),
            _ => null
        };

        // Skull storm only Tier 3
        if (tier == ShrineTier.Tier3)
            skullRoutine = StartCoroutine(SkullStormRoutine(GetSkullCount(tier)));
    }
    ProjectileModifiers GenerateRandomModifiers()
    {
        ProjectileModifiers mods = ProjectileModifiers.Default;

        // Speed variation
        mods.speedMultiplier = UnityEngine.Random.Range(0.7f, 1.4f);

        // Occasional wobble
        if (UnityEngine.Random.value < 0.35f)
        {
            mods.wobbleStrength = UnityEngine.Random.Range(0.2f, 0.6f);
            mods.wobbleFrequency = UnityEngine.Random.Range(3f, 7f);
        }

        // Rare delayed explosion
        if (UnityEngine.Random.value < 0.15f)
        {
            mods.explodeDelay = UnityEngine.Random.Range(0.5f, 1.2f);
        }

        return mods;
    }

    public void StopAllAttacks()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        if (skullRoutine != null) StopCoroutine(skullRoutine);
        attackRoutine = null;
        skullRoutine = null;
    }

    /* ===================== BULLET PATTERNS ===================== */

    IEnumerator RingRoutine(int bullets, float delay)
    {
        while (true)
        {
            FireRing(bullets);
            yield return new WaitForSeconds(delay);
        }
    }

    IEnumerator SpiralRoutine()
    {
        float angle = 0f;
        while (true)
        {
            angle += 12f;
            Fire(DirFromAngle(angle));
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator ChaosRoutine()
    {
        while (true)
        {
            FireRing(24);
            yield return new WaitForSeconds(0.4f);
            FireSpiralBurst();
            yield return new WaitForSeconds(0.3f);
        }
    }

    void FireRing(int bullets)
    {
        float step = 360f / bullets;
        for (int i = 0; i < bullets; i++)
            Fire(DirFromAngle(i * step));
    }

    void FireSpiralBurst()
    {
        for (int i = 0; i < 12; i++)
            Fire(DirFromAngle(UnityEngine.Random.Range(0f, 360f)));
    }

    void Fire(Vector2 dir)
    {
        if (ProjectilePool.Instance == null) return;
        if (shrine == null || gameState == null) return;

        var projectile = ProjectilePool.Instance.Get();
        if (projectile == null) return;

        int tension = gameState.RunState.runTension;

        ProjectileModifiers mods =
            ProjectileModifierFactory.ForShrineTier(
                shrine.currentTier,
                tension
            );

        projectile.transform.position = transform.position;

        projectile.Fire(
            dir,
            projectileSpeed,
            projectileLifetime,
            projectileDamage,
            mods
        );
    }






    /* ===================== SKULL STORM ===================== */

    IEnumerator SkullStormRoutine(int totalSkulls)
    {
        if (skullPrefab == null)
        {
            Debug.LogError("[SHRINE] Skull prefab missing");
            yield break;
        }

        List<SkullProjectile> skulls = new();

        // Spawn skulls across arena
        for (int i = 0; i < totalSkulls; i++)
        {
            Vector2 pos = GetRandomPosition();
            var skull = Instantiate(skullPrefab, pos, Quaternion.identity);
            skull.Drop(pos, armDelay, skullDamage);
            skulls.Add(skull);
        }

        yield return new WaitForSeconds(1f);

        // Arm in waves
        while (true)
        {
            ArmRandomSkulls(skulls);
            yield return new WaitForSeconds(armInterval);
        }
    }

    void ArmRandomSkulls(List<SkullProjectile> skulls)
    {
        skulls.RemoveAll(s => s == null);
        var available = skulls.FindAll(s => s.gameObject.activeSelf);

        for (int i = 0; i < armedPerWave && available.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, available.Count);
            available[index].Arm();
            available.RemoveAt(index);
        }
    }

    /* ===================== HELPERS ===================== */

    int GetSkullCount(ShrineTier tier) => tier switch
    {
        ShrineTier.Tier1 => skullTier1,
        ShrineTier.Tier2 => skullTier2,
        ShrineTier.Tier3 => skullTier3,
        _ => skullTier1
    };

    Vector2 DirFromAngle(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    Vector2 GetRandomPosition()
    {
        if (combatBounds == null)
            return (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * 4f;

        Bounds b = combatBounds.bounds;
        float pad = 0.5f;

        return new Vector2(
            UnityEngine.Random.Range(b.min.x + pad, b.max.x - pad),
            UnityEngine.Random.Range(b.min.y + pad, b.max.y - pad)
        );
    }
}
