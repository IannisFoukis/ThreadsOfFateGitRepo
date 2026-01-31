using UnityEngine;

public class EnemyAgent : MonoBehaviour
{
    // ─────────────────────────────
    // COORDINATOR (PUBLIC FOR LEGACY)
    // ─────────────────────────────
    [HideInInspector] public EncounterCoordinator coordinator;

    // ─────────────────────────────
    // ROLE
    // ─────────────────────────────
    [Header("Role")]
    public EnemyRole role;

    // ─────────────────────────────
    // FORMATION / SLOT
    // ─────────────────────────────
    [Header("Formation / Slot")]
    [SerializeField] private float slotArrivalThreshold = 0.25f;
    public float SlotArrivalThreshold => slotArrivalThreshold;

    // ─────────────────────────────
    // LEGACY / COMPATIBILITY
    // ─────────────────────────────
    public bool attackLock { get; set; }
    public int assignedSlot = -1;

    // ─────────────────────────────
    // CHAOS STATE (Phase H)
    // ─────────────────────────────
    [Header("Chaos State")]
    [SerializeField] private bool isChaotic;
    [SerializeField] private bool hasChaoticImpulse;

    public bool IsChaotic => isChaotic;
    public bool HasChaoticImpulse => hasChaoticImpulse;

    public void SetChaotic(bool value)
    {
        isChaotic = value;
    }

    public void TriggerChaoticImpulse()
    {
        hasChaoticImpulse = true;
    }

    public void ClearChaoticImpulse()
    {
        hasChaoticImpulse = false;
    }

    // ─────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────
    void Start()
    {
        if (coordinator == null)
            coordinator = FindFirstObjectByType<EncounterCoordinator>();

        if (coordinator != null)
            coordinator.Register(this);
    }

    void OnDisable()
    {
        if (coordinator != null)
            coordinator.Unregister(this);
    }

    // ─────────────────────────────
    // FORMATION API (CANONICAL)
    // ─────────────────────────────

    /// <summary>
    /// Where this enemy SHOULD be according to the coordinator
    /// </summary>
    public Vector3 GetFormationTarget()
    {
        return coordinator != null
            ? coordinator.GetWorldPositionFor(this)
            : transform.position;
    }

    /// <summary>
    /// Has the enemy reached its formation slot?
    /// </summary>
    public bool IsAtSlot(float threshold = -1f)
    {
        float t = threshold > 0 ? threshold : slotArrivalThreshold;
        return Vector3.Distance(transform.position, GetFormationTarget()) <= t;
    }

    /// <summary>
    /// Is the enemy currently moving between formation positions?
    /// </summary>
    public bool IsChangingFormation(float threshold = -1f)
    {
        if (coordinator == null)
            return false;

        float t = threshold > 0 ? threshold : slotArrivalThreshold;
        return Vector3.Distance(transform.position, GetFormationTarget()) > t;
    }
}