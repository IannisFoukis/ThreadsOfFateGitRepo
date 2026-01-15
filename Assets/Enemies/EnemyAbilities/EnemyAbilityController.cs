using System.Collections;
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
    [SerializeField] float shieldDuration = 0.6f;

    [Header("Offender")]
    [SerializeField] float dashForce = 6f;

    public EnemyAbilityTier currentTier = EnemyAbilityTier.Base;

    Transform player;

    void Awake()
    {
        roleController ??= GetComponent<EnemyRoleController>();
        health ??= GetComponent<Health>();
        player = FindAnyObjectByType<PlayerController>()?.transform;

        // Subscribe to spawn events so we can initialize when this GameObject is spawned
        GameEvents.OnEnemySpawned += OnEnemySpawned;
        // Subscribe to mid-fight escalation so abilities update during fights
        GameEvents.OnMidFightEscalation += OnMidFightEscalation;
    }

    void OnDestroy()
    {
        GameEvents.OnEnemySpawned -= OnEnemySpawned;
        GameEvents.OnMidFightEscalation -= OnMidFightEscalation;
    }

    void OnEnemySpawned(UnityEngine.GameObject enemy)
    {
        if (enemy != gameObject) return;

        // Ensure roleController reference is up to date
        roleController ??= GetComponent<EnemyRoleController>();
        health ??= GetComponent<Health>();
    }

    void OnMidFightEscalation()
    {
        // When the room triggers a mid-fight escalation, update ability tier accordingly
        if (roleController == null)
            roleController = GetComponent<EnemyRoleController>();

        if (roleController == null) return;

        // Determine current shrine tier (best-effort) and tension
        Shrine.ShrineTier shrineTier = Shrine.ShrineTier.Tier1;
        var shrine = FindAnyObjectByType<Shrine>();
        if (shrine != null) shrineTier = shrine.currentTier;

        int tension = 0;
        var gsm = FindAnyObjectByType<GameStateManager>();
        if (gsm != null && gsm.RunState != null) tension = gsm.RunState.runTension;

        ApplyEscalation(roleController.CurrentRole, shrineTier, tension, midFight: true);
    }

    // =====================================================
    // CALLED BY COMBAT ROOM / FSM / TIMERS
    // =====================================================
    public void TriggerAbility()
    {
        if (!player || roleController == null) return;

        switch (roleController.CurrentRole)
        {
            case EnemyRole.Melee:
                ExecuteOffender();
                break;

            case EnemyRole.Defender:
                ExecuteDefender();
                break;

            case EnemyRole.Charger:
                ExecuteSupport();
                break;
            case EnemyRole.Activator:
                ExecuteSupport();
                break;
            case EnemyRole.Elite:
                ExecuteSupport();
                break;
        }
    }

    // =====================================================
    // ESCALATION
    // =====================================================
    public void ApplyEscalation(
        EnemyRole role,
        ShrineTier shrineTier,
        int tension,
        bool midFight
    )
    {
        if (shrineTier == ShrineTier.Tier3 || tension >= 7)
            currentTier = EnemyAbilityTier.Elite;
        else if (shrineTier == ShrineTier.Tier2 || tension >= 4)
            currentTier = EnemyAbilityTier.Aggressive;
        else
            currentTier = EnemyAbilityTier.Base;

        Debug.Log($"[ABILITY] {role} → {currentTier}");
    }

    // =====================================================
    // OFFENDER
    // =====================================================
    void ExecuteOffender()
    {
        Vector2 dir = GetFireDirection();

        Fire(dir);

        if (currentTier >= EnemyAbilityTier.Aggressive)
        {
            Fire(Rotate(dir, 10f));
            Fire(Rotate(dir, -10f));
        }

        if (currentTier >= EnemyAbilityTier.Elite)
        {
            DashBurst();
        }
    }

    void DashBurst()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        transform.position += (Vector3)(dir * dashForce);
    }

    // =====================================================
    // DEFENDER
    // =====================================================
    void ExecuteDefender()
    {
        Fire(GetFireDirection());

        if (currentTier >= EnemyAbilityTier.Aggressive)
            ShieldAlly();

        if (currentTier >= EnemyAbilityTier.Elite)
            RadialBurst();
    }

    // =====================================================
    // SUPPORT
    // =====================================================
    void ExecuteSupport()
    {
        var ally = FindClosestAlly();
        if (!ally) return;

        BuffEnemy(ally, currentTier == EnemyAbilityTier.Elite);
    }

    // =====================================================
    // HELPERS
    // =====================================================
    void Fire(Vector2 dir)
    {
        // Runtime assert to help catch missing ProjectilePool in scenes
        if (ProjectilePool.Instance == null)
        {
            Debug.LogError("[ABILITY] ProjectilePool.Instance is null — ensure a ProjectilePool exists in the scene");
            return;
        }

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

    void BuffEnemy(GameObject target, bool elite)
    {
        var ability = target.GetComponent<EnemyAbilityController>();
        if (!ability) return;

        ability.projectileSpeed *= elite ? 1.5f : 1.2f;
    }

    void ShieldAlly()
    {
        var ally = FindClosestAlly();
        if (!ally) return;

        var h = ally.GetComponent<Health>();
        if (h)
            StartCoroutine(TemporaryInvulnerability(h, shieldDuration));
    }

    void RadialBurst()
    {
        for (int i = 0; i < 8; i++)
            Fire(Quaternion.Euler(0, 0, i * 45f) * Vector2.right);
    }

    IEnumerator TemporaryInvulnerability(Health h, float time)
    {
        h.SetInvulnerable(true);
        yield return new WaitForSeconds(time);
        h.SetInvulnerable(false);
    }
}
