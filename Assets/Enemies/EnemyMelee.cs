using UnityEngine;

public class EnemyMelee : MonoBehaviour
{
    public float attackRange = 1.5f;
    public float attackCooldown = 1.2f;
    public float attackVisualDuration = 0.25f; // how long enemy stays red

    private float lastAttackTime;
    private float attackVisualUntil;

    private EnemyAgent agent;
    private EncounterCoordinator coordinator;
    private Transform player;
    private SpriteRenderer sr;
    //private Color originalColor;

    [SerializeField] private Color attackColor = Color.red;
    [SerializeField] private float attackFlashTime = 0.35f;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();
        coordinator = agent != null ? agent.coordinator : null;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        sr = GetComponentInChildren<SpriteRenderer>();
       // if (sr != null) originalColor = sr.color;
    }

    void Update()
    {
        if (agent == null || coordinator == null || player == null)
            return;

        // Reset visual when attack window ends
        if (sr != null && Time.time > attackVisualUntil)
            sr.color = Color.white;

        // --- PHALANX GATES ---
        if (!agent.IsAtSlot()) return;
        if (agent.role != EnemyRole.Offender) return;
        if (!coordinator.IsHammer(agent)) return;

        if (coordinator.phalanxState == EncounterCoordinator.PhalanxState.BreakChase ||
            coordinator.phalanxState == EncounterCoordinator.PhalanxState.Collapse)
            return;

        TryAttackPlayer();
    }

    void TryAttackPlayer()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > attackRange)
            return;

        lastAttackTime = Time.time;

        // 🔴 VISUAL ATTACK SIGNAL
        if (sr != null)
        {
            sr.color = attackColor;
            attackVisualUntil = Time.time + attackFlashTime;
        }


        Debug.Log($"[PHALANX] HAMMER STRIKE → {name}");
    }
}
