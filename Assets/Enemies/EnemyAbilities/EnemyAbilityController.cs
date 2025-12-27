using UnityEngine;
using static Shrine;

public class EnemyAbilityController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] EnemyRoleController roleController;
    [SerializeField] Health health;

    [Header("Projectile")]
    [SerializeField] float projectileSpeed = 6f;
    [SerializeField] float projectileLifetime = 3f;
    [SerializeField] int projectileDamage = 1;

    [Header("Defender")]
    [SerializeField] float shieldDuration = 0.5f;

    [Header("Offender")]
    [SerializeField] float dashForce = 6f;

    public EnemyAbilityTier currentTier = EnemyAbilityTier.Base;

    Transform player;

    void Awake()
    {
        roleController ??= GetComponent<EnemyRoleController>();
        health ??= GetComponent<Health>();
        player = FindAnyObjectByType<PlayerController>()?.transform;
    }

    void Update()
    {
        ExecuteAbility();
    }

    // =====================================================
    // ESCALATION ENTRY POINT (THIS WAS MISSING)
    // =====================================================
    public void ApplyEscalation(
        EnemyRole role,
        ShrineTier shrineTier,
        int tension,
        bool midFight
    )
    {
        EnemyAbilityTier targetTier = EnemyAbilityTier.Base;

        if (shrineTier == ShrineTier.Tier3 || tension >= 7)
            targetTier = EnemyAbilityTier.Elite;
        else if (shrineTier == ShrineTier.Tier2 || tension >= 4)
            targetTier = EnemyAbilityTier.Aggressive;

        currentTier = targetTier;

        Debug.Log($"[ABILITY] {role} → Tier {currentTier} (midFight={midFight})");
    }

    // =====================================================
    // MAIN DISPATCH
    // =====================================================
    void ExecuteAbility()
    {
        if (!player || roleController == null) return;

        switch (roleController.role)
        {
            case EnemyRole.Offender:
                ExecuteOffender();
                break;

            case EnemyRole.Defender:
                ExecuteDefender();
                break;
        }
    }

    // ================= OFFENDER =================
    void ExecuteOffender()
    {
        Vector2 dir = GetFireDirection();

        // Tier 1
        Fire(dir);

        // Tier 2
        if (currentTier >= EnemyAbilityTier.Aggressive)
        {
            Fire(Rotate(dir, 10f));
            Fire(Rotate(dir, -10f));
        }

        // Tier 3
        if (currentTier >= EnemyAbilityTier.Elite)
        {
            DashThroughPlayer();
        }
    }

    void DashThroughPlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        transform.position += (Vector3)(dir * dashForce * Time.deltaTime);
    }

    // ================= DEFENDER =================
    void ExecuteDefender()
    {
        switch (currentTier)
        {
            case EnemyAbilityTier.Base:
                Fire(GetFireDirection());
                break;

            case EnemyAbilityTier.Aggressive:
                Fire(GetFireDirection());
                ShieldAlly();
                break;

            case EnemyAbilityTier.Elite:
                ShieldAlly();
                RadialBurst();
                break;
        }
    }

    void ShieldAlly()
    {
        var ally = FindClosestAlly();
        if (!ally) return;

        var allyHealth = ally.GetComponent<Health>();
        if (allyHealth)
            StartCoroutine(TemporaryInvulnerability(allyHealth, shieldDuration));
    }

    void RadialBurst()
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 dir = Quaternion.Euler(0, 0, i * 45f) * Vector2.right;
            Fire(dir);
        }
    }

    // ================= HELPERS =================
    void Fire(Vector2 dir)
    {
        if (!ProjectilePool.Instance) return;

        var p = ProjectilePool.Instance.Get();
        if (!p) return;

        p.transform.position = transform.position;
        p.Fire(dir, projectileSpeed, projectileLifetime, projectileDamage, ProjectileModifiers.Default);
    }

    Vector2 GetFireDirection()
    {
        return (player.position - transform.position).normalized;
    }

    Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(
            v.x * Mathf.Cos(rad) - v.y * Mathf.Sin(rad),
            v.x * Mathf.Sin(rad) + v.y * Mathf.Cos(rad)
        );
    }

    GameObject FindClosestAlly()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        float best = float.MaxValue;
        GameObject result = null;

        foreach (var e in enemies)
        {
            if (e.gameObject == gameObject) continue;
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < best)
            {
                best = d;
                result = e.gameObject;
            }
        }
        return result;
    }

    System.Collections.IEnumerator TemporaryInvulnerability(Health h, float time)
    {
        h.SetInvulnerable(true);
        yield return new WaitForSeconds(time);
        h.SetInvulnerable(false);
    }
}
