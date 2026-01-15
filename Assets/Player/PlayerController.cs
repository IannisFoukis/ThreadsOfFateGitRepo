using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
public class PlayerController : MonoBehaviour
{
    PlayerMotor motor;
    Vector2 input;
    bool movementLocked;

    public Vector2 MoveInput => input;

    void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        if (motor == null)
            Debug.LogError("[PlayerController] PlayerMotor missing!");
    }

    void Update()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
    }

    void FixedUpdate()
    {
        if (motor == null) return;

        if (movementLocked)
        {
            motor.SetInput(Vector2.zero);
            return;
        }

        motor.SetInput(input);
    }

    public void SetMovementLock(bool locked)
    {
        movementLocked = locked;
    }
}
