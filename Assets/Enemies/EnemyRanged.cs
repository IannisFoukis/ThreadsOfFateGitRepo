using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRanged : MonoBehaviour
{
    Rigidbody2D rb;
    EnemyAgent agent;
    Transform player;

    [Header("Fire Origin")]
    public Transform firePoint;
    Vector2 FireOrigin => firePoint != null ? (Vector2)firePoint.position : rb.position;

    [Header("Firing")]
    public Projectile projectilePrefab;
    public float fireCooldown = 1.6f;
    public float fireRange = 9f;
    public float projectileSpeed = 7f;
    public float projectileLifetime = 3f;
    public int damage = 1;
    public ProjectileModifiers modifiers;

    float lastFireTime;
    float losClearTime;
    public float losGraceDuration = 0.22f;

    [Header("Aim Telegraph")]
    public float aimTime = 0.55f;
    public float aimLineWidth = 0.05f;
    public Color aimColor = new Color(1f, 0.8f, 0.2f, 0.8f);

    LineRenderer aimLine;
    float aimTimer;
    bool isAiming;

    [Header("Reposition")]
    public float repositionCooldown = 2.0f;
    public float repositionSpeed = 8f;
    public float repositionDuration = 0.25f;

    float lastRepositionTime;
    bool isRepositioning;
    float repositionTimer;
    Vector2 repositionDir;

    [Header("Formation Lock")]
    public float formationLockTolerance = 2.0f;

    [Header("Fire LOS")]
    public LayerMask losMask;
    public LayerMask allyMask;
    public float allyBlockRadius = 0.18f;

    [Header("Vision")]
    public float visionRange = 11f;
    public LayerMask visionBlockMask;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();
        rb = GetComponent<Rigidbody2D>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

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

    void Update()
    {
       
        if (!agent.IsActivated)
            return;

        if (agent.attackPositionLocked)
            return;


        if (agent == null || player == null)
            return;

        // Never aim during Assemble
        if (agent.coordinator != null &&
            agent.coordinator.phalanxState == EncounterCoordinator.PhalanxState.Assemble)
        {
            HardResetAim();
            return;
        }

        if (agent.role != EnemyRole.Ranger)
            return;

        // 🔓 LOOSENED formation gate
        bool formationReady =
            agent.coordinator == null ||
            agent.coordinator.phalanxState != EncounterCoordinator.PhalanxState.Assemble;

        if (!formationReady)
        {
            HardResetAim();
            TryReposition();
            return;
        }

        if (!CanSeePlayer())
        {
            HardResetAim();
            TryReposition();
            return;
        }

        float drift = Vector2.Distance(rb.position, agent.GetFormationTarget());
        if (drift > formationLockTolerance)
        {
            HardResetAim();
            return;
        }

        if (!agent.attackPositionLocked)
            agent.SetAttackPositionLocked(true);

        if (Time.time < lastFireTime + fireCooldown)
        {
            HoldAim();
            return;
        }

        if (!isAiming)
            StartAim();

        aimTimer += Time.deltaTime;

        if (HasClearShotFrom(FireOrigin))
        {
            UpdateAimLine();
            losClearTime += Time.deltaTime;
        }
        else
        {
            aimLine.enabled = false;
            losClearTime = 0f;
        }

        if (aimTimer >= aimTime && losClearTime >= losGraceDuration)
            Fire();
    }

    // ─────────────────────────────
    // AIM / FIRE
    // ─────────────────────────────

    void UpdateAimLine()
    {
        aimLine.enabled = true;
        aimLine.SetPosition(0, FireOrigin);
        aimLine.SetPosition(1, player.position);
    }

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

    void HardResetAim()
    {
        isAiming = false;
        aimTimer = 0f;
        losClearTime = 0f;
        agent.movementLocked = false;
        agent.SetAttackPositionLocked(false);
        aimLine.enabled = false;
    }

    void Fire()
    {
        aimLine.enabled = false;
        agent.movementLocked = false;
        lastFireTime = Time.time;

        Vector2 dir = ((Vector2)player.position - FireOrigin).normalized;

        Instantiate(projectilePrefab, FireOrigin, Quaternion.identity)
            .Fire(dir, projectileSpeed, projectileLifetime, damage, modifiers);

        agent.SetAttackPositionLocked(false);
    }

    // ─────────────────────────────
    // LOS / VISION
    // ─────────────────────────────

    bool CanSeePlayer()
    {
        Vector2 origin = FireOrigin;
        Vector2 toPlayer = (Vector2)player.position - origin;

        if (toPlayer.magnitude > visionRange)
            return false;

        return !Physics2D.Raycast(
            origin,
            toPlayer.normalized,
            toPlayer.magnitude,
            visionBlockMask
        );
    }

    bool HasClearShotFrom(Vector2 origin)
    {
        Vector2 dir = ((Vector2)player.position - origin).normalized;
        float dist = Vector2.Distance(origin, player.position);

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, losMask);
        if (!hit || !hit.collider.CompareTag("Player"))
            return false;

        RaycastHit2D allyHit =
            Physics2D.CircleCast(origin, allyBlockRadius, dir, dist, allyMask);
        if (allyHit && allyHit.collider.CompareTag("Enemy"))
            return false;

        return true;
    }

    // ─────────────────────────────
    // REPOSITION
    // ─────────────────────────────

    void TryReposition()
    {
        if (Time.time < lastRepositionTime + repositionCooldown)
            return;

        lastRepositionTime = Time.time;
        Vector2 toPlayer = ((Vector2)player.position - rb.position).normalized;
        repositionDir = Vector2.Perpendicular(toPlayer) * (Random.value < 0.5f ? -1 : 1);
        repositionTimer = repositionDuration;
        isRepositioning = true;
    }

    void FixedUpdate()
    {
        if (!isRepositioning || isAiming) return;

        repositionTimer -= Time.fixedDeltaTime;
        rb.MovePosition(rb.position + repositionDir * repositionSpeed * Time.fixedDeltaTime);

        if (repositionTimer <= 0f)
            isRepositioning = false;
    }
}
