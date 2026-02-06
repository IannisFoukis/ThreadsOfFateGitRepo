using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyCharger : MonoBehaviour
{
    [Header("Charge")]
    public float chargeForce = 8f;
    public float chargeCooldown = 2f;

    [Header("Commit Gates")]
    public float minCommitDistance = 3.5f;   // 🔒 prevents long-range charges

    private Rigidbody2D rb;
    private Transform player;
    private EnemyStateController state;
    private EnemyAgent agent;
    private EncounterCoordinator coordinator;

    private bool canCharge = true;
    private bool sacrificeUsed = false;

    private DoctrineState doctrine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        state = GetComponent<EnemyStateController>();
        agent = GetComponent<EnemyAgent>();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (GameLock.IsLocked)
            return;

        if (!player || !canCharge || state.CurrentState == EnemyState.Dead)
            return;

        // Lazy resolve coordinator
        if (coordinator == null)
            coordinator = agent != null ? agent.coordinator : null;

        if (coordinator == null)
            return;

        // Cache doctrine once
        if (doctrine == null)
            doctrine = GetDoctrineFromCoordinator();

        // ─────────────────────────────────────────────
        // 🔒 FORMATION COMMIT AUTHORITY (CRITICAL)
        // ─────────────────────────────────────────────

        // Never charge during Assemble or Hold
        if (coordinator.phalanxState != EncounterCoordinator.PhalanxState.March)
            return;

        // Do not break formation while still moving to slot
        if (agent != null && agent.IsChangingFormation())
            return;

        // Distance gate – no long-range suicide charges
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > minCommitDistance)
            return;

        // ─────────────────────────────────────────────
        // SACRIFICE GATE (ONE-TIME)
        // ─────────────────────────────────────────────
        if (!sacrificeUsed && ShouldSacrifice())
        {
            StartCoroutine(SacrificeCharge());
        }
    }

    // ─────────────────────────────────────────────
    // SACRIFICE LOGIC
    // ─────────────────────────────────────────────

    bool ShouldSacrifice()
    {
        if (doctrine == null)
            return false;

        if (!doctrine.canSacrifice)
            return false;

        // Fanatics always commit
        if (doctrine.fanatic)
            return true;

        // Chaos increases chance
        float chance = doctrine.chaotic ? 0.15f : 0.05f;
        return Random.value < chance;
    }

    IEnumerator SacrificeCharge()
    {
        sacrificeUsed = true;
        canCharge = false;

        Debug.Log($"[OFFENDER] SACRIFICE CHARGE → {name}");

        // Lock state: offender is now committed and uncoordinated
        state.SetState(EnemyState.Hit);

        Vector2 dir = (player.position - transform.position).normalized;

        rb.linearVelocity = Vector2.zero;

        rb.AddForce(dir * chargeForce, ForceMode2D.Impulse);

        // Commitment window
        yield return new WaitForSeconds(0.5f);

        yield return new WaitForSeconds(chargeCooldown);
    }

    // ─────────────────────────────────────────────
    // DOCTRINE ACCESS (READ-ONLY)
    // ─────────────────────────────────────────────

    DoctrineState GetDoctrineFromCoordinator()
    {
        return coordinator != null
            ? coordinator.GetType()
                .GetField("doctrine",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                ?.GetValue(coordinator) as DoctrineState
            : null;
    }
}
