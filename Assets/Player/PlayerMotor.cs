using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    public float moveSpeed = 5f;

    Rigidbody2D rb;
    Vector2 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetInput(Vector2 moveInput)
    {
        input = moveInput;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * moveSpeed;
    }
}
