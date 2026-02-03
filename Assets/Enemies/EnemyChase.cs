using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float arriveDistance = 0.15f;

    [Header("Separation")]
    public float separationRadius = 1.6f;
    public float separationPushRadius = 1.1f;
    public float separationStrength = 1.5f;
    public LayerMask enemyLayer;

    [Header("Separation Priority (Phase N3)")]
    [SerializeField] private float defenderMass = 2.0f;
    [SerializeField] private float rangerMass = 0.7f;
    [SerializeField] private float offenderMass = 1.0f;

    [Header("Lane Priority (Phase N4)")]
    [SerializeField] private float frontLanePriority = 1.5f;
    [SerializeField] private float midLanePriority = 1.0f;
    [SerializeField] private float backLanePriority = 0.6f;
    [Header("Overlap Resolution (Phase N5)")]
    [SerializeField] private float overlapResolveStrength = 0.5f;
    [SerializeField] private float overlapMinDistance = 0.01f;
    [Header("Obstacle Avoidance (Phase N1)")]
    [SerializeField] private float obstacleRayDistance = 0.5f;
    [SerializeField] private float obstacleAvoidStrength = 0.35f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Obstacle Avoidance Bias (Phase N2)")]
    [SerializeField] private float nearSlotDistance = 0.9f;
    [SerializeField] private float nearSlotBiasMin = 0.25f;
    [SerializeField] private float rangerBias = 1.35f;
    [SerializeField] private float defenderBias = 0.65f;
    [SerializeField] private float offenderBias = 1.0f;

    private EnemyAgent agent;
    private Rigidbody2D rb;

    private Transform cachedPlayer;
    private float speedMultiplier = 1f;

    private float stunUntilTime = -1f;
    private float forceAggroUntilTime = -1f;

    private Vector2 currentDir = Vector2.right;
    public Vector2 CurrentDir => currentDir;

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

        if (Time.time < stunUntilTime)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector3 target = agent.GetFormationTarget();

        if (!IsFinite(target))
        {
            if (agent.coordinator != null && agent.coordinator.IsLeaderDead())
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            ResolvePlayerOnce();
            if (cachedPlayer == null) return;
            target = cachedPlayer.position;
        }

        Vector2 toTarget = (Vector2)(target - transform.position);

        if (toTarget.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 desiredDir = toTarget.normalized;

        Vector2 sep = GetSeparationForce();
        Vector2 avoid = ComputeObstacleAvoidance(desiredDir);

        Vector2 finalDir = desiredDir + sep + avoid;

        if (finalDir.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        finalDir.Normalize();
        currentDir = finalDir;

        rb.linearVelocity = finalDir * (moveSpeed * speedMultiplier);

        ResolveOverlaps();
    }

    // ───────── Phase N2 ─────────

    private Vector2 ComputeObstacleAvoidance(Vector2 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.001f)
            return Vector2.zero;

        Vector2 origin = rb.position;
        Vector2 dir = moveDir.normalized;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, obstacleRayDistance, obstacleMask);
        if (!hit) return Vector2.zero;

        Vector2 side = Vector2.Perpendicular(dir);

        bool leftBlocked = Physics2D.Raycast(origin, side, obstacleRayDistance * 0.75f, obstacleMask);
        bool rightBlocked = Physics2D.Raycast(origin, -side, obstacleRayDistance * 0.75f, obstacleMask);

        if (leftBlocked && rightBlocked)
            return Vector2.zero;

        if (leftBlocked && !rightBlocked)
            side = -side;

        float bias = 1f;

        Vector3 slotTarget = agent.GetFormationTarget();
        if (IsFinite(slotTarget))
        {
            float dist = Vector2.Distance(rb.position, (Vector2)slotTarget);
            if (dist < nearSlotDistance)
            {
                float t = Mathf.InverseLerp(0f, nearSlotDistance, dist);
                bias *= Mathf.Lerp(nearSlotBiasMin, 1f, t);
            }
        }

        bias *= GetRoleAvoidBias();

        return side.normalized * obstacleAvoidStrength * bias;
    }

    private float GetRoleAvoidBias()
    {
        if (agent == null) return offenderBias;

        switch (agent.role)
        {
            case EnemyRole.Defender: return defenderBias;
            case EnemyRole.Ranger: return rangerBias;
            default: return offenderBias;
        }
    }

    // ───────── Phase N3 + N4 ─────────

    private Vector2 GetSeparationForce()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position,
            separationRadius,
            enemyLayer
        );

        Vector2 force = Vector2.zero;
        int count = 0;

        float myMass = GetRoleMass(agent != null ? agent.role : EnemyRole.Offender);
        float myLane = agent != null ? GetLanePriority(agent.Lane) : backLanePriority;

        foreach (var col in nearby)
        {
            if (col == null || col.gameObject == gameObject) continue;

            var otherAgent = col.GetComponent<EnemyAgent>();
            if (otherAgent == null) continue;

            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist > separationPushRadius || dist < 0.0001f) continue;

            Vector2 away = ((Vector2)transform.position - (Vector2)col.transform.position).normalized;

            float otherMass = GetRoleMass(otherAgent.role);
            float otherLane = GetLanePriority(otherAgent.Lane);

            float massPriority = Mathf.Clamp(otherMass / myMass, 0.35f, 2.5f);
            float lanePriority = Mathf.Clamp(otherLane / myLane, 0.35f, 2.5f);

            force += away * (massPriority * lanePriority);
            count++;
        }

        if (count > 0) force /= count;
        return force * separationStrength;
    }

    private void ResolveOverlaps()
    {
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol == null) return;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            myCol.bounds.center,
            separationPushRadius,
            enemyLayer
        );

        float myMass = GetRoleMass(agent != null ? agent.role : EnemyRole.Offender);

        foreach (var col in nearby)
        {
            if (col == null || col.gameObject == gameObject) continue;

            var otherAgent = col.GetComponent<EnemyAgent>();
            if (otherAgent == null) continue;

            Collider2D otherCol = col;
            if (!myCol.bounds.Intersects(otherCol.bounds)) continue;

            Vector2 myPos = rb.position;
            Vector2 otherPos = otherCol.attachedRigidbody != null
                ? otherCol.attachedRigidbody.position
                : (Vector2)otherCol.transform.position;

            Vector2 delta = myPos - otherPos;
            float dist = delta.magnitude;

            if (dist < overlapMinDistance)
                delta = Random.insideUnitCircle.normalized;

            float otherMass = GetRoleMass(otherAgent.role);
            float totalMass = myMass + otherMass;

            float myShare = otherMass / totalMass;

            Vector2 correction = delta.normalized * overlapResolveStrength * myShare;

            rb.position += correction;
        }
    }
    private float GetRoleMass(EnemyRole role)
    {
        switch (role)
        {
            case EnemyRole.Defender: return defenderMass;
            case EnemyRole.Ranger: return rangerMass;
            default: return offenderMass;
        }
    }

    private float GetLanePriority(Lane lane)
    {
        // Front lane always has priority
        if (lane.ToString() == "Front")
            return frontLanePriority;

        // Any non-front lane yields
        return backLanePriority;
    }

    // ───────── Public API ─────────

    public void Stun(float duration)
    {
        stunUntilTime = Mathf.Max(stunUntilTime, Time.time + Mathf.Max(0f, duration));
        rb.linearVelocity = Vector2.zero;
    }

    public void ForceAggro(float duration)
    {
        forceAggroUntilTime = Mathf.Max(forceAggroUntilTime, Time.time + Mathf.Max(0f, duration));
        ResolvePlayerOnce();
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 0.05f, 10f);
    }

    public void ResetSpeed()
    {
        speedMultiplier = 1f;
    }

    // ───────── Helpers ─────────

    private void ResolvePlayerOnce()
    {
        if (cachedPlayer != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) cachedPlayer = go.transform;
    }

    private bool IsFinite(Vector3 v)
    {
        return !(float.IsNaN(v.x) || float.IsNaN(v.y) ||
                 float.IsInfinity(v.x) || float.IsInfinity(v.y));
    }
}