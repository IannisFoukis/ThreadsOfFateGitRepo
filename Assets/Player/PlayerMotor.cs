using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float dashDrag = 5f;
    public float defaultDashForce = 12f;

    private Rigidbody2D rb;

    private Vector2 moveInput;
    private Vector2 aimInput;
    private Vector2 dashVelocity;
    [SerializeField] private Transform visualRoot;

    public Vector2 FacingDir { get; private set; } = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        Vector2 velocity = moveInput * moveSpeed;

        if (dashVelocity != Vector2.zero)
        {
            velocity += dashVelocity;
            dashVelocity = Vector2.Lerp(
                dashVelocity,
                Vector2.zero,
                dashDrag * Time.fixedDeltaTime
            );
        }
        if (aimInput.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(aimInput.y, aimInput.x) * Mathf.Rad2Deg;
            visualRoot.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        rb.linearVelocity = velocity;
        UpdateFacing();
    }

    // =====================================
    // INPUT
    // =====================================

    // NEW (mouse / aim aware)
    public void SetInput(Vector2 move, Vector2 aim)
    {
        moveInput = move;
        aimInput = aim;
    }

    // OLD (movement only) — keeps PlayerController working
    public void SetInput(Vector2 move)
    {
        moveInput = move;
        aimInput = Vector2.zero;
    }

    // =====================================
    // DASH
    // =====================================

    // NEW (explicit force)
    public void ApplyDashVelocity(Vector2 direction, float force)
    {
        dashVelocity = direction.normalized * force;
    }

    // OLD (Skill_Dash compatibility)
    public void ApplyDashVelocity(Vector2 direction)
    {
        dashVelocity = direction.normalized * defaultDashForce;
    }

    public void ClearDashVelocity()
    {
        dashVelocity = Vector2.zero;
    }

    // =====================================
    // FACING
    // =====================================
    void UpdateFacing()
    {
        if (aimInput.sqrMagnitude > 0.01f)
            FacingDir = aimInput.normalized;
        else if (moveInput.sqrMagnitude > 0.01f)
            FacingDir = moveInput.normalized;
    }
}
