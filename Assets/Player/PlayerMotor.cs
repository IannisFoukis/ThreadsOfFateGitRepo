using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    public float moveSpeed = 6f;

    Rigidbody2D rb;
    Vector2 input;
    Vector2 dashVelocity;

    public bool IsDashing => dashVelocity.sqrMagnitude > 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void SetInput(Vector2 move)
    {
        input = Vector2.ClampMagnitude(move, 1f);
    }

    public void ApplyDashVelocity(Vector2 velocity)
    {
        dashVelocity = velocity;
    }

    public void ClearDashVelocity()
    {
        dashVelocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (dashVelocity.sqrMagnitude > 0f)
        {
            rb.linearVelocity = dashVelocity;
            return;
        }

        rb.linearVelocity = input * moveSpeed;
    }

}
