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

        // 🔒 Latch defender intent
        if (agent.role == EnemyRole.Defender)
            defenderIntentUntil = Time.time + defenderIntentDuration;

        if (Time.time > defenderIntentUntil)
            return;

        // 🔒 No block during BreakChase
        if (coordinator.phalanxState == EncounterCoordinator.PhalanxState.BreakChase)
            return;

        // 🔒 Functional slot check
        Vector2 slotPos = agent.GetFormationTarget();
        float slotDist = Vector2.Distance(transform.position, slotPos);
        if (slotDist > agent.slotArrivalThreshold * slotToleranceMultiplier)
            return;

        // 🔒 Player proximity
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > blockCheckRange)
            return;

        // 🔒 Facing check
        Vector2 toPlayer = (player.position - transform.position).normalized;
        Vector2 forward = transform.up; // assuming up is forward
        float angle = Vector2.Angle(forward, toPlayer);

        if (angle > blockAngleTolerance)
            return;

        ApplyBlock();
    }

    void ApplyBlock()
    {
        // Deny movement through defender
        playerMotor.externalForceActive = true;

        // Kill only forward component (no shove)
        Vector2 vel = playerMotor.GetVelocity();
        Vector2 blockNormal = (player.position - transform.position).normalized;
        vel -= Vector2.Dot(vel, blockNormal) * blockNormal;

        playerMotor.SetVelocity(vel);

        // 🔵 Visual: darker blue than shove
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
