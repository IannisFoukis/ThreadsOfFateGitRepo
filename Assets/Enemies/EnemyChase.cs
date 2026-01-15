using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    [SerializeField] float baseSpeed = 3f;

    Rigidbody2D rb;
    Transform player;
    EnemySlotLock slotLock;

    float speedMultiplier = 1f;
    public float Speed => baseSpeed * speedMultiplier;

    // 🔥 This is now FACING direction (used by anim / aim)
    public Vector2 CurrentDir { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        slotLock = GetComponent<EnemySlotLock>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    void FixedUpdate()
    {
        if (!player) return;

        // ---------- MOVEMENT TARGET ----------
        Vector2 moveTarget;

        if (slotLock != null && slotLock.HasSlot)
            moveTarget = slotLock.GetSlotPosition();
        else
            moveTarget = player.position;

        Vector2 moveDir = ((Vector2)moveTarget - rb.position);

        if (moveDir.sqrMagnitude > 0.01f)
            rb.linearVelocity = moveDir.normalized * Speed;
        else
            rb.linearVelocity = Vector2.zero;

        // ---------- FACING TARGET (ALWAYS PLAYER) ----------
        Vector2 faceDir = (player.position - transform.position);

        if (faceDir.sqrMagnitude > 0.001f)
            CurrentDir = faceDir.normalized;
    }

    // ---- API hooks (unchanged) ----
    public void SetSpeedMultiplier(float mult) => speedMultiplier = mult;
    public void ResetSpeed() => speedMultiplier = 1f;

    public void ForceAggro() { }
    public void ForceAggro(float duration) { ForceAggro(); }

    public void Stun(float duration)
    {
        rb.linearVelocity = Vector2.zero;
    }
}
