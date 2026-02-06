using UnityEngine;
using TOF.EnemyAI.Groups;

public class EnemyAgent : MonoBehaviour
{
    [HideInInspector] public EncounterCoordinator coordinator;

    // ───────── GROUP (Phase G1) ─────────
    public EnemyGroup Group { get; private set; }
    public bool IsLeader { get; private set; }

    public GroupIntent CurrentGroupIntent { get; private set; }
    public bool GroupPhalanxActive { get; private set; }
    public float GroupCooldownMult { get; private set; } = 1f;
    public float GroupAggressionMult { get; private set; } = 1f;
    public float GroupCohesionMult { get; private set; } = 1f;
    // ───────── LEGACY COMPATIBILITY (READ-ONLY) ─────────
    // These exist ONLY to avoid breaking older scripts.
    // They must NEVER be written to.

    public bool IsActivated => CombatEngaged;
    public bool squadActive => CombatEngaged;

    // ───────── Role ─────────
    [Header("Role")]
    public EnemyRole role;

    // ───────── Lane (LEGACY — C3 FROZEN) ─────────
    [Header("Lane (Legacy / Frozen)")]
    [SerializeField] private Lane lane = Lane.Front;
    public Lane Lane => lane;

    // ───────── Formation / Slot ─────────
    [Header("Formation / Slot")]
    [SerializeField] private float slotArrivalThreshold = 0.35f;
    public float SlotArrivalThreshold => slotArrivalThreshold;

    [HideInInspector] public bool attackPositionLocked;
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

    // ✅ Phase G1: single source of truth
    public bool CombatEngaged { get; private set; }

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

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

        // Phase G1 rule:
        // Spawned enemies are ALWAYS inactive until squad activation.
        CombatEngaged = false;
        movementLocked = true;
        attackPositionLocked = true;
    }

    void OnDestroy()
    {
        if (coordinator != null)
            coordinator.Unregister(this);
    }

    // ─────────────────────────────────────────────
    // PHASE G1 — SQUAD ACTIVATION RESET
    // ─────────────────────────────────────────────

    /// <summary>
    /// Called ONLY by EncounterCoordinator.
    /// This is the authoritative reset + engage boundary.
    /// </summary>
    public void ActivateForCombat()
    {
        // Formation / slot
        assignedSlot = -1;
        attackPositionLocked = false;
        attackLock = false;

        // Movement
        movementLocked = false;

        // Chaos residue
        hasChaoticImpulse = false;

        // Group memory (re-applied after activation)
        CurrentGroupIntent = GroupIntent.None;
        GroupPhalanxActive = false;
        GroupCooldownMult = 1f;
        GroupAggressionMult = 1f;
        GroupCohesionMult = 1f;

        // Leadership always reassigned explicitly
        IsLeader = false;

        SetCombatEngaged(true);

        Debug.Log($"[G1] Activated {name} (role={role})");
    }

    // ─────────────────────────────────────────────
    // COMBAT AUTHORITY
    // ─────────────────────────────────────────────

    public void SetCombatEngaged(bool value)
    {
        if (CombatEngaged == value) return;
        CombatEngaged = value;
        Debug.Log($"[CombatEngage] {name} -> {value}");
    }

    // ─────────────────────────────────────────────
    // CHAOS API
    // ─────────────────────────────────────────────

    public void SetChaotic(bool value) => isChaotic = value;
    public void TriggerChaoticImpulse() => hasChaoticImpulse = true;
    public void ClearChaoticImpulse() => hasChaoticImpulse = false;

    // ─────────────────────────────────────────────
    // ATTACK POSITION
    // ─────────────────────────────────────────────

    public void SetAttackPositionLocked(bool value)
    {
        if (attackPositionLocked == value) return;
        attackPositionLocked = value;
        Debug.Log($"[AttackPosLock] {name} role={role} -> {value}");
    }

    // ─────────────────────────────────────────────
    // FORMATION HELPERS
    // ─────────────────────────────────────────────

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
        if (coordinator == null) return false;

        float t = threshold > 0 ? threshold : slotArrivalThreshold;
        return Vector3.Distance(transform.position, GetFormationTarget()) > t;
    }

    public float DistanceToSlot()
    {
        return Vector3.Distance(transform.position, GetFormationTarget());
    }

    // Lane assignment intentionally disabled (C3 frozen)
    public void AssignLane(Lane newLane) { }

    // ─────────────────────────────────────────────
    // GROUP API
    // ─────────────────────────────────────────────

    public void AssignGroup(EnemyGroup group) => Group = group;
    public void SetLeaderFlag(bool isLeader) => IsLeader = isLeader;

    public void ApplyGroupDirectives(GroupDirectives directives)
    {
        CurrentGroupIntent = directives.intent;
        GroupPhalanxActive = directives.phalanxActive;

        GroupCooldownMult = Mathf.Max(0.1f, directives.scaling.cooldownMult);
        GroupAggressionMult = Mathf.Max(0.1f, directives.scaling.aggressionMult);
        GroupCohesionMult = Mathf.Max(0.1f, directives.scaling.cohesionMult);
    }
}
