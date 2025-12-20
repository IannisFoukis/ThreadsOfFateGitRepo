using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    Rigidbody2D rb;

    bool forceMoving;
    Vector2 forceDir;
    Vector2 moveInput;
    float forceSpeed;
    float forceTime;
    [Header("Movement")]
    [SerializeField] float moveSpeed = 6f;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    public void SetInput(Vector2 input)
    {
        moveInput = input;
    }
    public void ForceMove(Vector2 direction, float speed, float duration)
    {
        forceMoving = true;
        forceDir = direction.normalized;
        forceSpeed = speed;
        forceTime = duration;
        

    }

    void FixedUpdate()
    {
       
        if (forceMoving)
        {
            rb.linearVelocity = forceDir * forceSpeed;

            forceTime -= Time.fixedDeltaTime;
            if (forceTime <= 0f)
            {
                forceMoving = false;
                rb.linearVelocity = Vector2.zero;
            }

            return; // ⛔ skip normal movement while forcing
        }
        rb.linearVelocity = moveInput * moveSpeed;
        // normal movement handled elsewhere
    }
}
