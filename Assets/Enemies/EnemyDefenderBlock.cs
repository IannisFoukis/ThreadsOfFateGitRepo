using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyDefenderBlock : MonoBehaviour
{
    [Header("Block")]
    public float blockAngleTolerance = 70f;      // degrees
    public float blockCheckRange = 1.0f;

    [Header("Defender Intent")]
    public float defenderIntentDuration = 0.6f;
    public float slotToleranceMultiplier = 2.5f;

    [Header("Phase A - Commit Reaction")]
    [Tooltip("Multiplier applied to block range when an Offender is committing nearby")]
    public float commitCoverRangeMultiplier = 1.4f;

    private float defenderIntentUntil = -1f;

    private EnemyAgent agent;
    private EncounterCoordinator coordinator;
    private Transform player;
    private PlayerMotor playerMotor;
    private Collider2D col;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();
        col = GetComponent<Collider2D>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerMotor = p.GetComponent<PlayerMotor>();
        }
    }

    void Update()
    {
        if (agent == null || player == null || playerMotor == null)
            return;

        if (coordinator == null)
            coordinator = agent.coordinator;

        if (coordinator == null)
            return;

        // 🔒 Latch defender intent ONCE
        if (agent.role == EnemyRole.Defender && Time.time > defenderIntentUntil)
            defenderIntentUntil = Time.time + defenderIntentDuration;

        if (Time.time > defenderIntentUntil)
            return;

        // 🔒 No block during BreakChase
        if (coordinator.phalanxState == EncounterCoordinator.PhalanxState.BreakChase)
            return;

        // 🔒 Functional slot check
        Vector3 slotPos3 = agent.GetFormationTarget();
        Vector2 slotPos = new Vector2(slotPos3.x, slotPos3.y);

        float slotDist = Vector2.Distance(transform.position, slotPos);
        if (slotDist > agent.SlotArrivalThreshold * slotToleranceMultiplier)
            return;

        // 🔒 Player proximity (Phase A: extend range during Offender commit)
        float dist = Vector2.Distance(transform.position, player.position);

        float effectiveRange = blockCheckRange;
        if (OffenderIsCommittingNearby())
            effectiveRange *= commitCoverRangeMultiplier;

        if (dist > effectiveRange)
            return;

        // 🔒 Facing check
        Vector2 toPlayer = (player.position - transform.position).normalized;
        Vector2 forward = transform.up; // assuming up is forward
        float angle = Vector2.Angle(forward, toPlayer);

        if (angle > blockAngleTolerance)
            return;

        ApplyBlock();
    }

    // ─────────────────────────────
    // Phase A: Commit Awareness
    // ─────────────────────────────
    bool OffenderIsCommittingNearby(float range = 4f)
    {
        if (agent == null || agent.coordinator == null)
            return false;

        var enemies = agent.coordinator.GetEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null) continue;
            if (e.role != EnemyRole.Offender) continue;
            if (!e.attackLock) continue;

            float d = Vector2.Distance(e.transform.position, transform.position);
            if (d <= range)
                return true;
        }
        return false;
    }

    // ─────────────────────────────
    // Block Execution
    // ─────────────────────────────
    void ApplyBlock()
    {
        // Deny movement through defender
        playerMotor.externalForceActive = true;

        // Kill only forward component (no shove)
        Vector2 vel = playerMotor.GetVelocity();
        Vector2 blockNormal = (player.position - transform.position).normalized;
        vel -= Vector2.Dot(vel, blockNormal) * blockNormal;

        playerMotor.SetVelocity(vel);

        // 🔵 Visual feedback (block)
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0.2f, 0.4f, 1f);
            Invoke(nameof(ResetColor), 0.1f);
        }

        Invoke(nameof(ReleaseBlock), 0.05f);
    }

    void ReleaseBlock()
    {
        if (playerMotor != null)
            playerMotor.externalForceActive = false;
    }

    void ResetColor()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.white;
    }
}