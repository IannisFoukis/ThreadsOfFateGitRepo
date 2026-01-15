using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Visuals")]
    [Tooltip("Sprite root (child). Leave empty to rotate this transform.")]
    public Transform visualRoot;

    Rigidbody2D rb;

    Vector2 input;
    Vector2 dashVelocity;
    Vector2 lastMoveDir = Vector2.down; // default facing

    public bool IsDashing => dashVelocity.sqrMagnitude > 0.01f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (visualRoot == null)
            visualRoot = transform;
    }

    public void SetInput(Vector2 move)
    {
        input = Vector2.ClampMagnitude(move, 1f);

        if (input.sqrMagnitude > 0.01f)
            lastMoveDir = input.normalized;
    }

    public void ApplyDashVelocity(Vector2 velocity)
    {
        dashVelocity = velocity;

        if (velocity.sqrMagnitude > 0.01f)
            lastMoveDir = velocity.normalized;
    }

    public void ClearDashVelocity()
    {
        dashVelocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (IsDashing)
        {
            rb.linearVelocity = dashVelocity;
        }
        else
        {
            rb.linearVelocity = input * moveSpeed;
        }

        UpdateFacing();
    }

    void UpdateFacing()
    {
        if (lastMoveDir.sqrMagnitude < 0.01f)
            return;

        float angle = Mathf.Atan2(lastMoveDir.y, lastMoveDir.x) * Mathf.Rad2Deg;
        visualRoot.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }
}
