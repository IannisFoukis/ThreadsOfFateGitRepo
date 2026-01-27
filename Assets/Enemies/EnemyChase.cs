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

        // If stunned, stop.
        if (Time.time < stunUntilTime)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Prefer formation target ALWAYS (agent may still return a "best effort" target even before slot assignment)
        Vector3 target = agent.GetFormationTarget();

        // Safety fallback only if agent gives something unusable
        // Safety fallback ONLY if leader is alive
        if (!IsFinite(target))
        {
            // If leader is dead, NEVER collapse to player-chase
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

        // Arrive
        if (toTarget.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 desiredDir = toTarget.normalized;

        // Separation steering
        Vector2 sep = GetSeparationForce();
        Vector2 finalDir = (desiredDir + sep).normalized;

        currentDir = finalDir;

        float finalSpeed = moveSpeed * speedMultiplier;
        rb.linearVelocity = finalDir * finalSpeed;
    }

    // --- API expected by other scripts (Knockback/Health/EnemyFacing/etc.) ---

    public void Stun(float duration)
    {
        stunUntilTime = Mathf.Max(stunUntilTime, Time.time + Mathf.Max(0f, duration));
        rb.linearVelocity = Vector2.zero;
    }

    public void ForceAggro(float duration)
    {
        forceAggroUntilTime = Mathf.Max(forceAggroUntilTime, Time.time + Mathf.Max(0f, duration));
        ResolvePlayerOnce();
        // (Your combat logic can use forceAggroUntilTime if needed later)
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 0.05f, 10f);
    }

    public void ResetSpeed()
    {
        speedMultiplier = 1f;
    }

    // --- Helpers ---

    private void ResolvePlayerOnce()
    {
        if (cachedPlayer != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) cachedPlayer = go.transform;
    }

    private Vector2 GetSeparationForce()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyLayer);

        Vector2 force = Vector2.zero;
        int count = 0;

        foreach (var col in nearby)
        {
            if (col == null || col.gameObject == gameObject) continue;

            // Only separate from other enemies that have EnemyAgent (keeps it cheap + consistent)
            var other = col.GetComponent<EnemyAgent>();
            if (other == null) continue;

            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < separationPushRadius && dist > 0.0001f)
            {
                force += ((Vector2)transform.position - (Vector2)col.transform.position).normalized;
                count++;
            }
        }

        if (count > 0) force /= count;

        // Convert to a steering influence (not a teleport)
        return force * separationStrength;
    }

    private bool IsFinite(Vector3 v)
    {
        return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y));
    }
}
