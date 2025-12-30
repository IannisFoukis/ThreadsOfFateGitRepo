using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ActivatorRunner : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float stopDistance = 0.4f;

    Rigidbody2D rb;
    EnemyRoleController roleController;
    Shrine shrine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        roleController = GetComponent<EnemyRoleController>();
    }

    void Start()
    {
        // shrine can be assigned externally (preferred). Fall back to any shrine in scene.
        if (shrine == null)
            shrine = FindAnyObjectByType<Shrine>();
    }

    // Allow external assignment of the target shrine (e.g., by CombatRoom when spawning)
    public void SetShrine(Shrine s)
    {
        shrine = s;
    }

    void FixedUpdate()
    {
        if (roleController == null || roleController.role != EnemyRole.Activator)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (shrine == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toShrine = (Vector2)shrine.transform.position - rb.position;

        if (toShrine.magnitude <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = toShrine.normalized * moveSpeed;
    }
}
