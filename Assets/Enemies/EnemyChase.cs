using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Arrival")]
    public float arriveDistance = 0.3f;
    public float rangerArriveDistance = 1f;
    public float offenderArriveDistance = 0.8f;

    [Tooltip("Within this distance, speed eases down to prevent orbit/overshoot.")]
    public float slowRadius = 2f;

    [Header("Chase Bias (Phase G1)")]
    [Tooltip("How much enemies bias movement toward the player while in formation")]
    public float chaseBiasStrength = 0.65f;

    [Tooltip("Prevents stutter when formationReady flickers.")]
    public float formationReadyGrace = 0.20f;

    [Header("Separation")]
    public float separationPushRadius = 1.8f;
    public float separationStrength = 0.9f;
    public float separationRadius = 2.2f;

    [Header("Enemy Layers")]
    public LayerMask enemyLayer;
    public LayerMask enemyMask;

    [Header("Separation Priority")]
    [SerializeField] private float defenderMass = 4f;
    [SerializeField] private float rangerMass = 0.7f;
    [SerializeField] private float offenderMass = 1.0f;

    [Header("Lane Priority")]
    [SerializeField] private float frontLanePriority = 1.5f;
    [SerializeField] private float backLanePriority = 0.6f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleRayDistance = 0.5f;
    [SerializeField] private float obstacleAvoidStrength = 0.35f;
    [SerializeField] private LayerMask obstacleMask;

    private EnemyAgent agent;
    private Rigidbody2D rb;
    private Transform cachedPlayer;

    private float lastFormationReadyTime = -999f;

    LayerMask EnemyMaskResolved => enemyLayer.value != 0 ? enemyLayer : enemyMask;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        agent = GetComponent<EnemyAgent>();
    }

    void Start()
    {
        ResolvePlayerOnce();
    }

    void FixedUpdate()
    {
        
        if (!agent.squadActive)
            return;
        if (!agent.IsActivated)
            return;
        if (agent.movementLocked)
            return;

        if (agent == null) return;

        // Absolute movement ownership
        if (agent.movementLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        ResolvePlayerOnce();

        Vector3 slot3 = agent.GetFormationTarget();
        if (!IsFinite(slot3))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 slot = (Vector2)slot3;
        Vector2 toSlot = slot - rb.position;

        // -----------------------------
        // FORMATION READY (with grace)
        // -----------------------------
        bool formationReadyRaw =
            agent.coordinator == null ||
            OffendersAreEngaged() || // once frontline is fighting, stop forcing perfection
            !agent.coordinator.AnyRoleChangingFormation(
                EnemyRole.Ranger, // rangers can be moving while others still advance
                agent.SlotArrivalThreshold * 1.1f
            );

        if (formationReadyRaw) lastFormationReadyTime = Time.time;

        bool formationReady =
            formationReadyRaw ||
            (Time.time - lastFormationReadyTime) <= formationReadyGrace;

        // Hard stop if locked in attack position
        if (agent.attackPositionLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float arrive = arriveDistance;
        if (agent.role == EnemyRole.Ranger) arrive = rangerArriveDistance;
        else if (agent.role == EnemyRole.Offender) arrive = offenderArriveDistance;

        float distToSlot = toSlot.magnitude;

        float distToPlayer = float.MaxValue;
        if (cachedPlayer != null)
            distToPlayer = Vector2.Distance(rb.position, cachedPlayer.position);

        // -----------------------------
        // MOVEMENT DIRECTION
        // -----------------------------
        Vector2 desiredDir = (distToSlot > 0.001f) ? toSlot.normalized : Vector2.zero;

        // Add a forward bias to keep the whole group advancing (prevents “slot hover” hesitation)
        if (formationReady && cachedPlayer != null)
        {
            Vector2 chaseBias = ComputeChaseBias();
            Vector2 combined = desiredDir + chaseBias;

            if (combined.sqrMagnitude > 0.0001f)
                desiredDir = combined.normalized;
        }

        // -----------------------------
        // ARRIVAL / STOP CONDITIONS
        // -----------------------------
        // If very close to slot, don't instantly stop if we're still far from player.
        // (This is the classic "they reach slot but group doesn't chase".)
        bool atSlot = distToSlot <= arrive;

        if (atSlot)
        {
            // Let them keep creeping forward unless already near the player
            // (rangers are allowed to “sit” more, offenders least).
            float keepPressureDist = agent.role switch
            {
                EnemyRole.Offender => 2.4f,
                EnemyRole.Defender => 2.8f,
                EnemyRole.Ranger => 4.8f,
                _ => 3.0f
            };

            if (cachedPlayer == null || distToPlayer <= keepPressureDist)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
            // else: continue moving using chase-bias combined direction
        }

        Vector2 sep = GetSeparationForce(slot);
        Vector2 avoid = ComputeObstacleAvoidance(desiredDir);

        Vector2 finalDir = desiredDir + sep + avoid;
        if (finalDir.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        finalDir.Normalize();

        float ease = Mathf.Clamp01(distToSlot / Mathf.Max(0.01f, slowRadius));
        float speed = moveSpeed * Mathf.Lerp(0.35f, 1f, ease);

        rb.linearVelocity = finalDir * speed;
    }

    // ─────────────────────────────
    // Chase Bias
    // ─────────────────────────────
    Vector2 ComputeChaseBias()
    {
        if (cachedPlayer == null) return Vector2.zero;

        Vector2 toPlayer = ((Vector2)cachedPlayer.position - rb.position);
        if (toPlayer.sqrMagnitude < 0.001f)
            return Vector2.zero;

        float roleWeight = agent.role switch
        {
            EnemyRole.Offender => 1.0f,
            EnemyRole.Defender => 0.65f,
            EnemyRole.Ranger => 0.35f, // raise this later if you want rangers to drift forward more
            _ => 0.5f
        };

        return toPlayer.normalized * (chaseBiasStrength * roleWeight);
    }

    // “Frontline engaged” = stop over-gating formation perfection.
    bool OffendersAreEngaged()
    {
        if (agent == null || agent.coordinator == null || cachedPlayer == null)
            return false;

        var enemies = agent.coordinator.GetEnemies();
        float engageDist = 2.2f;

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null) continue;
            if (e.role != EnemyRole.Offender) continue;

            float d = Vector2.Distance(e.transform.position, cachedPlayer.position);
            if (d <= engageDist)
                return true;
        }

        return false;
    }

    // ─────────────────────────────
    // Separation / Avoidance
    // ─────────────────────────────
    Vector2 GetSeparationForce(Vector2 slotTarget)
    {
        float distToSlot = Vector2.Distance(rb.position, slotTarget);
        if (distToSlot < 0.6f)
            return Vector2.zero;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position,
            separationRadius,
            EnemyMaskResolved
        );

        Vector2 force = Vector2.zero;
        int count = 0;

        float myMass = GetRoleMass(agent.role);
        float myLane = GetLanePriority(agent.Lane);

        foreach (var c in nearby)
        {
            if (c == null || c.gameObject == gameObject) continue;

            var other = c.GetComponent<EnemyAgent>();
            if (other == null) continue;

            float d = Vector2.Distance(transform.position, c.transform.position);
            if (d > separationPushRadius || d < 0.0001f) continue;

            Vector2 away = ((Vector2)transform.position - (Vector2)c.transform.position).normalized;

            float otherMass = GetRoleMass(other.role);
            float otherLane = GetLanePriority(other.Lane);

            float massPriority = Mathf.Clamp(otherMass / myMass, 0.35f, 2.5f);
            float lanePriority = Mathf.Clamp(otherLane / myLane, 0.35f, 2.5f);

            force += away * (massPriority * lanePriority);
            count++;
        }

        if (count > 0) force /= count;
        return force * separationStrength;
    }

    Vector2 ComputeObstacleAvoidance(Vector2 moveDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(rb.position, moveDir, obstacleRayDistance, obstacleMask);
        if (!hit) return Vector2.zero;

        return Vector2.Perpendicular(moveDir).normalized * obstacleAvoidStrength;
    }

    float GetRoleMass(EnemyRole role) =>
        role switch
        {
            EnemyRole.Defender => defenderMass,
            EnemyRole.Ranger => rangerMass,
            _ => offenderMass
        };

    float GetLanePriority(Lane lane) =>
        lane == Lane.Front ? frontLanePriority : backLanePriority;

    void ResolvePlayerOnce()
    {
        if (cachedPlayer != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) cachedPlayer = go.transform;
    }

    bool IsFinite(Vector3 v) =>
        !(float.IsNaN(v.x) || float.IsInfinity(v.x) ||
          float.IsNaN(v.y) || float.IsInfinity(v.y));

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}
