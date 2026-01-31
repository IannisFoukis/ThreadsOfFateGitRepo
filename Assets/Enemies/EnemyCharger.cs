using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyCharger : MonoBehaviour
{
    [Header("Charge")]
    public float chargeForce = 8f;
    public float chargeCooldown = 2f;

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

        Debug.Log($"[DEFENDER] SACRIFICE CHARGE → {name}");

        // Lock state: defender is now committed and uncoordinated
        state.SetState(EnemyState.Hit);

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dir * chargeForce, ForceMode2D.Impulse);

        // Commitment window
        yield return new WaitForSeconds(0.5f);

        // After sacrifice, defender becomes reckless by BEHAVIOR:
        // - no retreat
        // - no further coordination logic
        // (Update loop will naturally stop triggering anything else)

        yield return new WaitForSeconds(chargeCooldown);
    }

    // ─────────────────────────────────────────────
    // DOCTRINE ACCESS (READ-ONLY)
    // ─────────────────────────────────────────────

    DoctrineState GetDoctrineFromCoordinator()
    {
        return coordinator != null
            ? coordinator.GetType()
                .GetField("doctrine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(coordinator) as DoctrineState
            : null;
    }
}