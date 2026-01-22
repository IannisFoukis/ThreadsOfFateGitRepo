using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerMotor motor;

    Vector2 moveInput;
    Vector2 aimInput;

    public Vector2 MoveInput => moveInput;
    public Vector2 AimInput => aimInput;

    void Awake()
    {
        motor = GetComponent<PlayerMotor>();
    }

    void Update()
    {
        ReadInput();

        bool tactical = Input.GetKey(KeyCode.LeftShift);

        motor.overrideFacing = tactical;

        if (tactical)
        {
            ApplyTacticalFacing();
        }

        motor.SetInput(moveInput, aimInput);
    }

    void ApplyTacticalFacing()
    {
        Vector2 forward = motor.FacingDir.sqrMagnitude > 0.01f
            ? motor.FacingDir
            : moveInput;

        if (forward.sqrMagnitude < 0.01f)
            return;

        EnemyAgent target = FindBestFacingEnemy(
            transform.position,
            forward,
            12f
        );

        if (target != null)
        {
            Vector2 dir =
                (Vector2)(target.transform.position - transform.position);

            aimInput = dir.normalized;
        }
    }

    void ReadInput()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        // 🔑 ONLY read mouse when NOT in tactical mode
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            Vector3 mouseWorld =
                Camera.main.ScreenToWorldPoint(Input.mousePosition);

            Vector2 mouseDir =
                (Vector2)(mouseWorld - transform.position);

            aimInput = mouseDir.sqrMagnitude > 0.01f
                ? mouseDir.normalized
                : Vector2.zero;
        }
    }


    EnemyAgent FindBestFacingEnemy(
        Vector2 playerPos,
        Vector2 playerForward,
        float maxRange = 12f)
    {
        EnemyAgent best = null;
        float bestScore = float.MaxValue;

        EnemyAgent[] enemies = Object.FindObjectsByType<EnemyAgent>(
            FindObjectsSortMode.None
        );

        foreach (var enemy in enemies)
        {
            if (enemy == null)
                continue;

            Vector2 toEnemy =
                (Vector2)enemy.transform.position - playerPos;

            float distance = toEnemy.magnitude;

            if (distance > maxRange)
                continue;

            Vector2 dir = toEnemy.normalized;
            float dot = Vector2.Dot(playerForward.normalized, dir);

            float angleWeight = Mathf.Lerp(
                2.0f,   // behind penalty
                0.5f,   // front bonus
                (dot + 1f) * 0.5f
            );

            float score = distance * angleWeight;

            if (score < bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best;
    }
}
