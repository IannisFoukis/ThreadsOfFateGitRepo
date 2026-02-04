using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Arrival")]
    public float arriveDistance = 0.15f;
    public float rangerArriveDistance = 0.6f;
    public float offenderArriveDistance = 0.45f;

    [Tooltip("Within this distance, speed eases down to prevent orbit/overshoot.")]
    public float slowRadius = 1.25f;

    [Header("Separation")]
    public float separationRadius = 1.6f;
    public float separationPushRadius = 1.1f;
    public float separationStrength = 1.5f;
    public LayerMask enemyLayer;

    [Header("Separation Priority")]
    [SerializeField] private float defenderMass = 2.0f;
    [SerializeField] private float rangerMass = 0.7f;
    [SerializeField] private float offenderMass = 1.0f;

    [Header("Lane Priority")]
    [SerializeField] private float frontLanePriority = 1.5f;
    [SerializeField] private float backLanePriority = 0.6f;

    [Header("Overlap Resolution")]
    [SerializeField] private float overlapResolveStrength = 0.5f;
    [SerializeField] private float overlapMinDistance = 0.01f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleRayDistance = 0.5f;
    [SerializeField] private float obstacleAvoidStrength = 0.35f;
    [SerializeField] private LayerMask obstacleMask;

    private EnemyAgent agent;
    private Rigidbody2D rb;
    private Transform cachedPlayer;

    private float speedMultiplier = 1f;

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
        if (agent == null) return;
        // 🔒 ABSOLUTE STOP — engagement owns movement
        if (agent.attackPositionLocked || agent.attackLock)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        ResolvePlayerOnce();

        Vector3 target3 = agent.GetFormationTarget();
        if (!IsFinite(target3))
        {
            if (cachedPlayer == null)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
            target3 = cachedPlayer.position;
        }

        Vector2 target = (Vector2)target3;
        Vector2 toTarget = target - rb.position;

        float formationTolerance = agent.SlotArrivalThreshold * 3f;
        float distToSlot = toTarget.magnitude;
        float distToPlayer = cachedPlayer != null
            ? Vector2.Distance(rb.position, cachedPlayer.position)
            : float.MaxValue;

        float engageDistance = GetAttackEngageDistance();

        // ─────────────────────────────────────────────
        // 🔒 ATTACK POSITION LOCK (MUST HAPPEN FIRST)
        // ─────────────────────────────────────────────
        if (!agent.attackPositionLocked &&
            distToSlot <= formationTolerance &&
            distToPlayer <= engageDistance)
        {
            agent.SetAttackPositionLocked(true);
            rb.linearVelocity = Vector2.zero;

            // DEBUG (throttled)
            if (Time.frameCount % 20 == 0)
            {
                Debug.Log(
                    $"[LOCKED:{name}] role={agent.role} " +
                    $"distSlot={distToSlot:F2}/{formationTolerance:F2} " +
                    $"distPlayer={distToPlayer:F2}/{engageDistance:F2}"
                );
            }

            return;
        }

        // 🔒 HARD STOP IF LOCKED
        if (agent.attackPositionLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 🔒 HARD STOP IF MOVEMENT LOCKED
        if (agent.movementLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // ─────────────────────────────────────────────
        // ROLE-SPECIFIC FREEZES (AFTER LOCK CHECK)
        // ─────────────────────────────────────────────

        // Rangers: freeze while aiming
        if (agent.role == EnemyRole.Ranger)
        {
            var lr = GetComponent<LineRenderer>();
            if (lr != null && lr.enabled)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        // Offenders: close-range commit stop (movement only, lock handled above)
        if (agent.role == EnemyRole.Offender && cachedPlayer != null)
        {
            var off = GetComponent<EnemyOffenderAttack>();
            if (off != null)
            {
                float dToPlayer = Vector2.Distance(rb.position, cachedPlayer.position);
                if (dToPlayer <= off.forceCommitDistance * 1.05f)
                {
                    rb.linearVelocity = Vector2.zero;
                    return;
                }
            }
        }

        // ─────────────────────────────────────────────
        // ARRIVAL (navigation only)
        // ─────────────────────────────────────────────

        float arrive = arriveDistance;
        if (agent.role == EnemyRole.Ranger) arrive = rangerArriveDistance;
        else if (agent.role == EnemyRole.Offender) arrive = offenderArriveDistance;

        if (toTarget.sqrMagnitude <= arrive * arrive)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // ─────────────────────────────────────────────
        // STEERING
        // ─────────────────────────────────────────────

        Vector2 desiredDir = toTarget.normalized;
        Vector2 sep = GetSeparationForce(target);
        Vector2 avoid = ComputeObstacleAvoidance(desiredDir);

        Vector2 finalDir = desiredDir + sep + avoid;
        if (finalDir.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        finalDir.Normalize();

        float dist = toTarget.magnitude;
        float ease = Mathf.Clamp01(dist / Mathf.Max(0.01f, slowRadius));
        float finalSpeed = (moveSpeed * speedMultiplier) * Mathf.Lerp(0.35f, 1f, ease);

        rb.linearVelocity = finalDir * finalSpeed;

        ResolveOverlaps();
    }
    // ─────────────────────────────────────────────
    // Separation
    // ─────────────────────────────────────────────

    private Vector2 GetSeparationForce(Vector2 slotTarget)
    {
        // Disable separation very close to the slot target to prevent tangential orbit
        float distToSlot = Vector2.Distance(rb.position, slotTarget);
        if (distToSlot < 0.6f)
            return Vector2.zero;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position,
            separationRadius,
            enemyLayer
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

    // ─────────────────────────────────────────────
    // Overlaps
    // ─────────────────────────────────────────────

    private void ResolveOverlaps()
    {
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol == null) return;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            myCol.bounds.center,
            separationPushRadius,
            enemyLayer
        );

        float myMass = GetRoleMass(agent.role);

        foreach (var c in nearby)
        {
            if (c == null || c.gameObject == gameObject) continue;

            var other = c.GetComponent<EnemyAgent>();
            if (other == null) continue;

            Collider2D otherCol = c;
            if (!myCol.bounds.Intersects(otherCol.bounds)) continue;

            Vector2 otherPos = otherCol.attachedRigidbody != null
                ? otherCol.attachedRigidbody.position
                : (Vector2)otherCol.transform.position;

            Vector2 delta = rb.position - otherPos;
            float dist = delta.magnitude;

            if (dist < overlapMinDistance)
                delta = Random.insideUnitCircle.normalized;

            float otherMass = GetRoleMass(other.role);
            float total = myMass + otherMass;

            float myShare = otherMass / total;

            rb.position += delta.normalized * overlapResolveStrength * myShare;
        }
    }

    // ─────────────────────────────────────────────
    // Obstacle Avoidance (simple)
    // ─────────────────────────────────────────────

    private Vector2 ComputeObstacleAvoidance(Vector2 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.001f)
            return Vector2.zero;

        RaycastHit2D hit = Physics2D.Raycast(rb.position, moveDir, obstacleRayDistance, obstacleMask);
        if (!hit) return Vector2.zero;

        Vector2 side = Vector2.Perpendicular(moveDir).normalized;
        return side * obstacleAvoidStrength;
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private float GetRoleMass(EnemyRole role)
    {
        return role switch
        {
            EnemyRole.Defender => defenderMass,
            EnemyRole.Ranger => rangerMass,
            _ => offenderMass
        };
    }

    private float GetLanePriority(Lane lane)
    {
        return lane.ToString() == "Front"
            ? frontLanePriority
            : backLanePriority;
    }

    private void ResolvePlayerOnce()
    {
        if (cachedPlayer != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) cachedPlayer = go.transform;
    }

    private bool IsFinite(Vector3 v)
    {
        return !(float.IsNaN(v.x) || float.IsInfinity(v.x) ||
                 float.IsNaN(v.y) || float.IsInfinity(v.y));
    }
    float GetAttackEngageDistance()
    {
        return agent.role switch
        {
            EnemyRole.Offender => 1.8f,
            EnemyRole.Ranger => 6.5f,
            EnemyRole.Defender => 1.5f,
            _ => 2.0f
        };
    }
}