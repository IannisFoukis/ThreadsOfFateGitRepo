using UnityEngine;

public class PlayerController : MonoBehaviour
{
    PlayerMotor motor;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        motor = GetComponent<PlayerMotor>();
    }

    void Update()
    {
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        motor.SetInput(input.normalized);
    }
}
