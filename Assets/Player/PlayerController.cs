using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerMotor motor;

    Vector2 moveInput;
    Vector2 aimInput;

    // ✅ READ-ONLY ACCESSORS (this fixes your error)
    public Vector2 MoveInput => moveInput;
    public Vector2 AimInput => aimInput;

    void Awake()
    {
        motor = GetComponent<PlayerMotor>();
    }

    void Update()
    {
        ReadInput();
        motor.SetInput(moveInput, aimInput);
    }

    void ReadInput()
    {
        // Movement
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        // Mouse aim
        Vector3 mouseWorld =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector2 mouseDir =
            (Vector2)(mouseWorld - transform.position);

        aimInput = mouseDir.sqrMagnitude > 0.01f
            ? mouseDir.normalized
            : Vector2.zero;
    }
}
