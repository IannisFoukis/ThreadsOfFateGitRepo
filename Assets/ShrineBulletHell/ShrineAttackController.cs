using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Shrine;

public class ShrineAttackController : MonoBehaviour
{
    [Header("Combat Bounds")]
    [SerializeField] Collider2D combatBounds;

    [Header("Projectile Stats")]
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

    private RunDirector runDirector;

    [SerializeField] GameStateManager gsm;
   
    Coroutine attackRoutine;
    Coroutine skullRoutine;
    float spiralAngle;

    public enum ShrinePattern
    {
        Ring,
        Spiral,
        RandomBurst,
        Wall
    }
    public enum ShrineProjectilePattern
    {
        Ring,
        Spiral,
        Wall,
        RandomRain
    }

    void Awake()
    {
        gsm = FindAnyObjectByType<GameStateManager>();
        runDirector = FindAnyObjectByType<RunDirector>();
    }

    /* ===================== ENTRY ===================== */

    public void StartAttacksForTier(ShrineTier tier)
    {
        StopAllAttacks();

        attackRoutine = tier switch
        {
            ShrineTier.Tier1 => StartCoroutine(RingRoutine()),
            ShrineTier.Tier2 => StartCoroutine(SpiralRoutine()),
            ShrineTier.Tier3 => StartCoroutine(ChaosRoutine()),
            _ => null
        };

        if (tier == ShrineTier.Tier3)
            skullRoutine = StartCoroutine(SkullStormRoutine(GetSkullCount(tier)));
    }

    public void StopAllAttacks()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        if (skullRoutine != null) StopCoroutine(skullRoutine);
        attackRoutine = null;
        skullRoutine = null;
    }

    /* ===================== PATTERNS ===================== */

    IEnumerator RingRoutine()
    {
        while (true)
        {
            FireRing(12 + gsm.RunState.runTension);
            yield return new WaitForSeconds(GetFireDelay());
        }
    }

    IEnumerator SpiralRoutine()
    {
        while (true)
        {
            spiralAngle += 14f;
            Fire(DirFromAngle(spiralAngle));
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator ChaosRoutine()
    {
        while (true)
        {
            FireRing(24);
            yield return new WaitForSeconds(0.4f);
            FireRandomBurst();
            yield return new WaitForSeconds(0.3f);
        }
    }

    /* ===================== FIRING ===================== */
    void FireProjectile(Vector2 dir, Vector3? overridePos = null)
    {
        if (ProjectilePool.Instance == null) return;

        var p = ProjectilePool.Instance.Get();
        if (p == null) return;

        p.transform.position =
            overridePos ?? transform.position;

        var mods = ProjectileModifierFactory.ForShrineTier(
    shrine.currentTier,
    gsm.RunState.runTension
);



        p.Fire(
            dir.normalized,
            projectileSpeed,
            projectileLifetime,
            projectileDamage,
            mods
        );
    }

    void FireRing(int count)
    {
        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = step * i;
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            FireProjectile(dir);
        }
    }

    void FireSpiral(int count)
    {
        for (int i = 0; i < count; i++)
        {
            spiralAngle += 15f;

            Vector2 dir = new Vector2(
                Mathf.Cos(spiralAngle * Mathf.Deg2Rad),
                Mathf.Sin(spiralAngle * Mathf.Deg2Rad)
            );

            FireProjectile(dir);
        }
    }
    void FireWall(int count)
    {
        float width = 6f;
        float step = width / (count - 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos =
                transform.position +
                Vector3.right * (-width / 2 + step * i);

            FireProjectile(Vector2.down, spawnPos);
        }
    }

    void FireRandomBurst()
    {
        for (int i = 0; i < 6; i++)
            Fire(Random.insideUnitCircle.normalized);
    }

    void Fire(Vector2 dir)
    {
        if (ProjectilePool.Instance == null || shrine == null || gsm == null)
            return;

        var projectile = ProjectilePool.Instance.Get();
        if (!projectile) return;

        projectile.transform.position = transform.position;

        var mods = ProjectileModifierFactory.ForShrineTier(
            shrine.currentTier,
            gsm.RunState.runTension
        );

        projectile.Fire(
            dir,
            projectileSpeed,
            projectileLifetime,
            projectileDamage,
            mods
        );
    }
    public void FirePattern()
    {
        switch (GetPattern())
        {
            case ShrineProjectilePattern.Ring:
                FireRing(12);
                break;

            case ShrineProjectilePattern.Spiral:
                FireSpiral(10);
                break;

            case ShrineProjectilePattern.Wall:
                FireWall(8);
                break;
        }
    }

    /* ===================== SKULL STORM ===================== */

    IEnumerator SkullStormRoutine(int total)
    {
        var skulls = new List<SkullProjectile>();

        for (int i = 0; i < total; i++)
        {
            Vector2 pos = GetRandomPosition();
            var skull = Instantiate(skullPrefab, pos, Quaternion.identity);
            skull.Drop(pos, armDelay, skullDamage);
            skulls.Add(skull);
        }

        yield return new WaitForSeconds(1f);

        while (true)
        {
            ArmRandomSkulls(skulls);
            yield return new WaitForSeconds(armInterval);
        }
    }

    void ArmRandomSkulls(List<SkullProjectile> skulls)
    {
        skulls.RemoveAll(s => s == null);

        for (int i = 0; i < armedPerWave && skulls.Count > 0; i++)
        {
            int idx = Random.Range(0, skulls.Count);
            skulls[idx].Arm();
            skulls.RemoveAt(idx);
        }
    }

    /* ===================== HELPERS ===================== */

    float GetFireDelay()
    {
        float baseDelay = shrine.currentTier switch
        {
            ShrineTier.Tier1 => 1.2f,
            ShrineTier.Tier2 => 0.8f,
            ShrineTier.Tier3 => 0.5f,
            _ => 1f
        };

        return Mathf.Max(0.2f, baseDelay - gsm.RunState.runTension * 0.05f);
    }

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
        if (!combatBounds)
            return (Vector2)transform.position + Random.insideUnitCircle * 4f;

        Bounds b = combatBounds.bounds;
        float pad = 0.5f;

        return new Vector2(
            Random.Range(b.min.x + pad, b.max.x - pad),
            Random.Range(b.min.y + pad, b.max.y - pad)
        );
    }

    ShrineProjectilePattern GetPattern()
    {
        return shrine.currentTier switch
        {
            ShrineTier.Tier1 => ShrineProjectilePattern.Ring,
            ShrineTier.Tier2 => ShrineProjectilePattern.Spiral,
            ShrineTier.Tier3 => ShrineProjectilePattern.Wall,
            _ => ShrineProjectilePattern.Ring
        };
    }

}
