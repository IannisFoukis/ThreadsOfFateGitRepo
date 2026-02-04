using UnityEngine;

public class EnemyRanged : MonoBehaviour
{
    // ───────── Doctrine (read-only) ─────────
    DoctrineState doctrine;

    [Header("Firing")]
    public Projectile projectilePrefab;
    public float fireCooldown = 1.8f;
    public float fireRange = 9f;
    public float projectileSpeed = 7f;
    public float projectileLifetime = 3f;
    public int damage = 1;
    public ProjectileModifiers modifiers;
    float losClearTime;
    public float losGraceDuration = 0.25f;
    [Header("Aim Telegraph")]
    public float aimTime = 0.6f;
    public float aimLineWidth = 0.05f;
    public Color aimColor = new Color(1f, 0.8f, 0.2f, 0.8f);

    [Header("Reposition")]
    public float fleeDistance = 2.5f;
    public float repositionCooldown = 2.0f;

    public float repositionSpeed = 8f;
    public float repositionDuration = 0.25f;

    [Header("Cadence")]
    public float pressureBonusCooldown = 0.6f;

    [Header("Silence Phase")]
    public float silenceCooldownMultiplier = 1.5f;

    // ───────── Phase A – Ranger Intelligence ─────────
    [Header("Phase A - Ranger Intelligence")]
    public bool enablePhaseA = true;
    public float dashSpamThresholdPerSecond = 1.0f;
    public float dashDenyLeadDistance = 1.6f;
    public float tacticalPunishAimMult = 0.75f;
    public float offenderDisciplineRange = 6.0f;

    [Header("Phase A - Lock Conditions")]
    public float formationLockTolerance = 2.0f;

    // ───────── Line of Sight ─────────
    [Header("Line of Sight")]
    public LayerMask losMask;     // Player + Environment
    public LayerMask allyMask;    // Enemies
    public float allyBlockRadius = 0.25f;

    float lastFireTime;
    float aimTimer;
    bool isAiming;
    float lastRepositionTime;

    bool isRepositioning;
    float repositionTimer;
    Vector2 repositionDir;

    EnemyAgent agent;
    EncounterCoordinator coordinator;
    Transform player;

    PlayerBehaviorTracker behavior;
    PlayerController playerController;

    LineRenderer aimLine;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        behavior = FindFirstObjectByType<PlayerBehaviorTracker>();
        playerController = FindFirstObjectByType<PlayerController>();

        aimLine = gameObject.AddComponent<LineRenderer>();
        aimLine.enabled = false;
        aimLine.positionCount = 2;
        aimLine.startWidth = aimLineWidth;
        aimLine.endWidth = aimLineWidth;
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = aimColor;
        aimLine.endColor = aimColor;
        aimLine.sortingLayerName = "Ground";
        aimLine.sortingOrder = 0;
    }

    void OnDisable()
    {
        if (agent != null)
        {
            agent.movementLocked = false;
            agent.SetAttackPositionLocked(false);
        }
        CancelAimVisualOnly();
    }

    void Update()
    {
        if (agent == null || player == null)
        {
            CancelAimVisualOnly();
            return;
        }

        if (coordinator == null)
            coordinator = agent.coordinator;

        if (coordinator == null || agent.role != EnemyRole.Ranger)
        {
            CancelAimVisualOnly();
            return;
        }

        // ───── Range + formation eligibility ─────
        float distToPlayer = Vector2.Distance(transform.position, player.position);
        if (distToPlayer > fireRange)
        {
            ReleaseLocksAndAim();
            return;
        }

        float distToSlot = Vector2.Distance(transform.position, agent.GetFormationTarget());
        if (distToSlot > formationLockTolerance || agent.IsChangingFormation())
        {
            ReleaseLocksAndAim();
            return;
        }

        if (!agent.attackPositionLocked)
            agent.SetAttackPositionLocked(true);

        // Cooldown
        if (Time.time < lastFireTime + fireCooldown)
        {
            HoldAim();
            return;
        }

        // AIM
        if (!isAiming)
            StartAim();

        aimTimer += Time.deltaTime;
        UpdateAimLine();

        // 🔥 FIRE ONLY WITH CLEAR LOS
        if (HasClearShot())
        {
            losClearTime += Time.deltaTime;
        }
        else
        {
            losClearTime = 0f;
        }

        if (aimTimer >= aimTime && losClearTime >= losGraceDuration)
        {
            Fire();
        }
    }

    // ───────── LINE OF SIGHT ─────────

    bool HasClearShot()
    {
        Vector2 origin = transform.position;
        Vector2 target = player.position;
        Vector2 dir = (target - origin).normalized;
        float dist = Vector2.Distance(origin, target);

        // Wall / environment check
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, losMask);
        if (!hit || !hit.collider.CompareTag("Player"))
            return false;

        // Ally block check
        RaycastHit2D allyHit = Physics2D.CircleCast(
            origin,
            allyBlockRadius,
            dir,
            dist,
            allyMask
        );

        if (allyHit && allyHit.collider.CompareTag("Enemy"))
            return false;

        return true;
    }

    // ───────── AIM CONTROL ─────────

    void StartAim()
    {
        isAiming = true;
        aimTimer = 0f;
        agent.movementLocked = true;
        aimLine.enabled = true;
    }

    void HoldAim()
    {
        agent.movementLocked = true;
        isAiming = true;
    }

    void CancelAimVisualOnly()
    {
        isAiming = false;
        aimTimer = 0f;
        if (aimLine != null)
            aimLine.enabled = false;
    }

    void ReleaseLocksAndAim()
    {
        isAiming = false;
        aimTimer = 0f;
        agent.movementLocked = false;
        agent.SetAttackPositionLocked(false);
        if (aimLine != null)
            aimLine.enabled = false;
    }

    void UpdateAimLine()
    {
        if (!aimLine.enabled) return;
        aimLine.SetPosition(0, transform.position);
        aimLine.SetPosition(1, player.position);
    }

    // ───────── FIRE ─────────

    void Fire()
    {
        CancelAimVisualOnly();
        agent.movementLocked = false;
        lastFireTime = Time.time;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        proj.Fire(dir, projectileSpeed, projectileLifetime, damage, modifiers);

        agent.SetAttackPositionLocked(false);
    }

    // ───────── DEBUG ─────────
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = HasClearShot() ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, player.position);
    }

    DoctrineState GetDoctrine()
    {
        if (doctrine != null) return doctrine;
        if (coordinator == null) return null;

        doctrine = coordinator.GetType()
            .GetField("doctrine",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
            ?.GetValue(coordinator) as DoctrineState;

        return doctrine;
    }
}