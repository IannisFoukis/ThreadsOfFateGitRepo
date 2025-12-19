using UnityEngine;

public class PlayerController : MonoBehaviour
{
    
    [Header("Core Components")]
    [SerializeField] PlayerMotor motor;
    [SerializeField] Health health;

    public PlayerMotor Motor => motor;
    public Health Health => health;

    public Vector2 LastMoveDir { get; private set; } = Vector2.right;
    Vector2 input;
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
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // 🔒 LOCK LAST MOVE DIR
        if (input.sqrMagnitude > 0.01f)
            LastMoveDir = input.normalized;
    }
    void FixedUpdate()
    {
        motor.SetInput(input.normalized);
    }

}
