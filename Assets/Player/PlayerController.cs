using UnityEngine;

public class PlayerController : MonoBehaviour
{
    
    [Header("Core Components")]
    [SerializeField] PlayerMotor motor;
    [SerializeField] Health health;

    public PlayerMotor Motor => motor;
    public Health Health => health;

    public Vector2 LastMoveDir { get; private set; }
    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (motor == null)
            motor = GetComponent<PlayerMotor>();

        if (health == null)
            health = GetComponent<Health>();
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
