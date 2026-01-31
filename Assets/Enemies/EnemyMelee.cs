using UnityEngine;

public class EnemyMelee : MonoBehaviour
{
    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1.2f;

    private float lastAttackTime;

    private EnemyAgent agent;
    private EncounterCoordinator coordinator;
    private Transform player;

    [Header("Visual")]
    [SerializeField] private Color attackColor = Color.red;
    [SerializeField] private float attackFlashTime = 0.35f;
    private float attackVisualUntil;
    private SpriteRenderer sr;

    // ─────────────────────────────────────────────
    // DOCTRINE (READ-ONLY, FROM ENCOUNTER)
    // ─────────────────────────────────────────────
    private DoctrineState doctrine;

    // Chaotic timing instability
    private float baseAttackCooldown;
    private float chaoticNextOffset = 0f;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        sr = GetComponentInChildren<SpriteRenderer>();

        baseAttackCooldown = attackCooldown;
    }

    void Update()
    {
        if (agent == null || player == null)
            return;

        // 🔁 Lazy resolve coordinator
        if (coordinator == null)
            coordinator = agent.coordinator;

        if (coordinator == null)
            return;

        // Cache doctrine once available
        if (doctrine == null)
            doctrine = GetDoctrineFromCoordinator();

        // Reset visual
        if (sr != null && Time.time > attackVisualUntil)
            sr.color = Color.white;

        // ─────────────────────────────────────────────
        // DOCTRINE-DRIVEN GATES
        // ─────────────────────────────────────────────

        // Encircle = no melee pressure (unless chaos leaks)
        if (coordinator.phalanxState == EncounterCoordinator.PhalanxState.Encircle &&
            !IsChaoticImpulse())
            return;

        // Collapse = no coordinated melee (everyone panics)
        if (coordinator.phalanxState == EncounterCoordinator.PhalanxState.Collapse)
            return;

        // Role gate
        if (agent.role != EnemyRole.Offender)
            return;

        // Hammer gate (chaos allows insubordination)
        if (!coordinator.IsHammer(agent) && !IsChaoticImpulse())
            return;

        TryAttackPlayer();
    }

    void TryAttackPlayer()
    {
        float effectiveCooldown = attackCooldown;

        // 🌪 Chaos destabilizes timing
        if (IsChaoticImpulse())
        {
            effectiveCooldown = baseAttackCooldown + chaoticNextOffset;
            effectiveCooldown = Mathf.Max(0.25f, effectiveCooldown);
        }

        if (Time.time < lastAttackTime + effectiveCooldown)
            return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > attackRange)
            return;

        lastAttackTime = Time.time;

        // Re-roll chaos AFTER each strike
        if (IsChaoticImpulse())
            chaoticNextOffset = Random.Range(-0.6f, 0.8f);

        // Visual tell
        if (sr != null)
        {
            sr.color = attackColor;
            attackVisualUntil = Time.time + attackFlashTime;
        }

        Debug.Log($"[MELEE] Strike → {name}");
    }

    // ─────────────────────────────────────────────
    // DOCTRINE HELPERS (NO MUTATION)
    // ─────────────────────────────────────────────

    DoctrineState GetDoctrineFromCoordinator()
    {
        return coordinator != null
            ? coordinator.GetType()
                .GetField("doctrine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(coordinator) as DoctrineState
            : null;
    }

    bool IsChaoticImpulse()
    {
        if (doctrine == null)
            return false;

        // Global chaos OR local agent chaos
        return doctrine.chaotic || agent.IsChaotic;
    }
}