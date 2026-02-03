using UnityEngine;

public class EnemyAgent : MonoBehaviour
{
    [HideInInspector] public EncounterCoordinator coordinator;

    [Header("Role")]
    public EnemyRole role;

    [Header("Lane")]
    [SerializeField] private Lane lane;
    public Lane Lane => lane;

    [Header("Formation / Slot")]
    [SerializeField] private float slotArrivalThreshold = 0.25f;
    public float SlotArrivalThreshold => slotArrivalThreshold;

    // Legacy / compatibility
    public bool attackLock { get; set; }
    public int assignedSlot = -1;

    // Chaos (Phase H)
    [Header("Chaos State")]
    [SerializeField] private bool isChaotic;
    [SerializeField] private bool hasChaoticImpulse;

    public bool IsChaotic => isChaotic;
    public bool HasChaoticImpulse => hasChaoticImpulse;

    public void SetChaotic(bool value) => isChaotic = value;
    public void TriggerChaoticImpulse() => hasChaoticImpulse = true;
    public void ClearChaoticImpulse() => hasChaoticImpulse = false;

    void Start()
    {
        if (!RolePermissionBus.IsRoleAllowed(role))
        {
            Debug.Log($"[EnemyAgent] {name} role {role} not allowed in this room. Removing.");

            // Notify combat room BEFORE destroying
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
    public void AssignLane(Lane newLane)
    {
        lane = newLane;
    }
}