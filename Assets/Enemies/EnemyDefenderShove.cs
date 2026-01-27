using UnityEngine;
using System.Collections;   
public class EnemyDefenderShove : MonoBehaviour
{
    [Header("Shove")]
    public float shoveRange = 1.2f;
    public float shoveForce = 6f;
    public float shoveCooldown = 2.0f;

    [Header("Defender Intent")]
    public float defenderIntentDuration = 0.6f;

    [Header("Slot Tolerance")]
    public float slotToleranceMultiplier = 2.5f;

    private float lastShoveTime;
    private float defenderIntentUntil = -1f;

    private EnemyAgent agent;
    private EncounterCoordinator coordinator;
    private Transform player;
    private Rigidbody2D playerRb;
    private SpriteRenderer sr;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();
        sr = GetComponentInChildren<SpriteRenderer>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerRb = p.GetComponent<Rigidbody2D>();
        }
    }

    void Update()
    {
        // 🔁 Lazy resolve coordinator (CRITICAL FIX)
        if (coordinator == null && agent != null)
            coordinator = agent.coordinator;

        if (agent == null || coordinator == null || player == null || playerRb == null)
            return;

        // 🔒 Latch defender intent when role becomes Defender
        if (agent.role == EnemyRole.Defender)
            defenderIntentUntil = Time.time + defenderIntentDuration;

        // 🔒 No active defender intent
        if (Time.time > defenderIntentUntil)
            return;

        // 🔒 Never shove during BreakChase
        if (coordinator.phalanxState == EncounterCoordinator.PhalanxState.BreakChase)
            return;

        // 🔒 Cooldown
        if (Time.time < lastShoveTime + shoveCooldown)
            return;

        // 🔒 Functional slot arrival (NOT pixel-perfect)
        Vector2 slotPos = agent.GetFormationTarget();
        float slotDist = Vector2.Distance(transform.position, slotPos);
        if (slotDist > agent.slotArrivalThreshold * slotToleranceMultiplier)
            return;

        // 🔒 Player proximity
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > shoveRange)
            return;

        ExecuteShove();
    }

    void ExecuteShove()
    {
        lastShoveTime = Time.time;
        Time.timeScale = 0.92f;
        Invoke(nameof(ResetTimeScale), 0.05f);
       // Camera.main.transform.position += (Vector3)(-dir * 0.15f);

        Vector2 dir = (player.position - transform.position).normalized;

        // Displacement only
        StartCoroutine(ApplyShove(dir));


        // 🔵 Defender visual intent
        if (sr != null)
        {
            sr.color = Color.blue;
            Invoke(nameof(ResetColor), 0.2f);
        }

        Debug.Log($"[DEFENDER] SHOVE → {name}");
    }
    void ResetTimeScale()
    {
        Time.timeScale = 1f;
    }

    IEnumerator ApplyShove(Vector2 dir)
    {
        float shoveTime = 0.12f;
        float timer = 0f;

        var playerMotor = player.GetComponent<PlayerMotor>();
        if (playerMotor != null)
            playerMotor.externalForceActive = true;

        float originalDamping = playerRb.linearDamping;
        playerRb.linearDamping = 0f;

        while (timer < shoveTime)
        {
            playerRb.linearVelocity = dir * shoveForce;
            timer += Time.deltaTime;
            yield return null;
        }

        playerRb.linearDamping = originalDamping;

        if (playerMotor != null)
            playerMotor.externalForceActive = false;
    }



    void ResetColor()
    {
        if (sr != null)
            sr.color = Color.white;
    }

    void OnDrawGizmosSelected()
    {
        // Shove range (BLUE)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, shoveRange);

        if (agent == null) return;

        // Slot tolerance zone (CYAN)
        Vector2 slotPos = agent.GetFormationTarget();
        float slotThresh = agent.slotArrivalThreshold * slotToleranceMultiplier;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(slotPos, slotThresh);
    }

}
