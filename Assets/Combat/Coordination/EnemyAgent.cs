using UnityEngine;

public class EnemyAgent : MonoBehaviour
{
    [HideInInspector] public EncounterCoordinator coordinator;

    // ───────── Role ─────────
    [Header("Role")]
    public EnemyRole role;

    // ───────── Lane (LEGACY — C3 FROZEN) ─────────
    [Header("Lane (Legacy / Frozen)")]
    [SerializeField] private Lane lane = Lane.Front;

    /// <summary>
    /// Legacy compatibility only.
    /// Lane has NO spatial authority in Phase C3.
    /// </summary>
    public Lane Lane => lane;

    // ───────── Formation / Slot ─────────
    [Header("Formation / Slot")]
    [SerializeField] private float slotArrivalThreshold = 0.35f;
    public float SlotArrivalThreshold => slotArrivalThreshold;

    // 🔒 Phase A+ — Attack Position Authority
    [HideInInspector] public bool attackPositionLocked;

    // Legacy / compatibility (do not use for new logic)
    public bool attackLock { get; set; }
    public int assignedSlot = -1;

    // ───────── Chaos (Phase H) ─────────
    [Header("Chaos State")]
    [SerializeField] private bool isChaotic;
    [SerializeField] private bool hasChaoticImpulse;

    public bool IsChaotic => isChaotic;
    public bool HasChaoticImpulse => hasChaoticImpulse;

    // ───────── Movement Authority ─────────
    [HideInInspector] public bool movementLocked;

    public void SetChaotic(bool value) => isChaotic = value;
    public void TriggerChaoticImpulse() => hasChaoticImpulse = true;
    public void ClearChaoticImpulse() => hasChaoticImpulse = false;

    // ───────── Attack Position Lock (Authoritative) ─────────
    public void SetAttackPositionLocked(bool value)
    {
        if (attackPositionLocked == value)
            return;

        attackPositionLocked = value;
        Debug.Log($"[AttackPosLock] {name} role={role} -> {value}");
    }

    // ───────── Lifecycle ─────────
    void Start()
    {
        // Role gating (room authority)
        if (!RolePermissionBus.IsRoleAllowed(role))
        {
            Debug.Log($"[EnemyAgent] {name} role {role} not allowed in this room. Removing.");

            var combatRoom = FindFirstObjectByType<CombatRoom>();
            if (combatRoom != null)
                combatRoom.NotifyEnemyRejected();

            Destroy(gameObject);
            return;
        }

        if (coordinator == null)
            coordinator = FindFirstObjectByType<EncounterCoordinator>();

        if (coordinator != null)
            coordinator.Register(this);
    }

    void OnDestroy()
    {
        if (coordinator != null)
            coordinator.Unregister(this);
    }

    // ───────── Formation API ─────────

    public Vector3 GetFormationTarget()
    {
        return coordinator != null
            ? coordinator.GetWorldPositionFor(this)
            : transform.position;
    }

    public bool IsAtSlot(float threshold = -1f)
    {
        float t = threshold > 0 ? threshold : slotArrivalThreshold;
        return Vector3.Distance(transform.position, GetFormationTarget()) <= t;
    }

    public bool IsChangingFormation(float threshold = -1f)
    {
        if (coordinator == null)
            return false;

        float t = threshold > 0 ? threshold : slotArrivalThreshold;
        return Vector3.Distance(transform.position, GetFormationTarget()) > t;
    }

    // ───────── Lane Assignment (DISABLED) ─────────
    /// <summary>
    /// Legacy stub. Lane assignment is disabled in Phase C3.
    /// This exists only to avoid breaking older callers.
    /// </summary>
    public void AssignLane(Lane newLane)
    {
        // Intentionally ignored
        // Lane has no authority in Phase C3
    }

    public float DistanceToSlot()
    {
        return Vector3.Distance(transform.position, GetFormationTarget());
    }

}
