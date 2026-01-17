using UnityEngine;

public class EnemyAgent : MonoBehaviour
{
    public EnemySlotType? assignedSlot;
    public EncounterCoordinator coordinator;

    [Header("Slot Behavior")]
    public float slotArrivalThreshold = 0.5f;
    public float smoothing = 6f;

    private Vector3 smoothedTarget;

    void Start()
    {
        if (coordinator == null)
        {
            coordinator = Object.FindFirstObjectByType<EncounterCoordinator>();
        }

        if (coordinator != null)
        {
            coordinator.Register(this);
        }
    }

    void OnDisable()
    {
        if (coordinator != null)
            coordinator.Unregister(this);
    }

    public bool HasAssignedSlot()
    {
        return assignedSlot.HasValue;
    }

    public bool IsAtSlot()
    {
        if (!assignedSlot.HasValue || coordinator == null)
            return false;

        Vector3 target = GetFormationTarget();
        return Vector3.Distance(transform.position, target) <= slotArrivalThreshold;
    }

    public Vector3 GetSmoothedTarget()
    {
        Vector3 target = GetFormationTarget();

        smoothedTarget = Vector3.Lerp(
            smoothedTarget,
            target,
            Time.deltaTime * smoothing
        );

        return smoothedTarget;
    }

    public Vector3 GetFormationTarget()
    {
        if (!assignedSlot.HasValue || coordinator == null)
            return transform.position;

        Vector3 basePos = coordinator.GetWorldPositionFor(this);

        // Deterministic jitter so multiple enemies on same slot don't stack
        int hash = Mathf.Abs(GetInstanceID());

        float xOffset = ((hash % 7) - 3) * 0.6f;
        float yOffset = ((hash % 5) - 2) * 0.6f;

        Vector3 jitter = new Vector3(xOffset, yOffset, 0);

        Vector3 target = basePos + jitter;

        // Add small avoidance from nearby allies
        target += GetAvoidanceOffset();

        return target;
    }

    private Vector3 GetAvoidanceOffset()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 1.2f);

        Vector3 avoid = Vector3.zero;
        int count = 0;

        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject)
                continue;

            EnemyAgent other = col.GetComponent<EnemyAgent>();

            if (other != null)
            {
                avoid += (transform.position - other.transform.position);
                count++;
            }
        }

        if (count > 0)
            avoid /= count;

        return avoid * 0.5f;
    }

    void OnDrawGizmos()
    {
        if (coordinator == null)
            return;

        if (!assignedSlot.HasValue)
            return;

        Vector3 target = GetFormationTarget();

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(target, 0.2f);
    }
}
