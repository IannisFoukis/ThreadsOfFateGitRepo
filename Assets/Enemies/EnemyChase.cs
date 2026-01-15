using UnityEngine;
using System.Collections;

public class EnemyChase : MonoBehaviour
{
    [SerializeField] float baseSpeed = 2.5f;

    float speedMultiplier = 1f;
    Rigidbody2D rb;
    Transform player;

    Vector2 currentDir;
    bool stunned;
    Coroutine stunRoutine;
    Coroutine aggroRoutine;

    public Vector2 CurrentDir => currentDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void FixedUpdate()
    {
        if (!player || stunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        currentDir = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = currentDir * baseSpeed * speedMultiplier;
    }

    // =====================
    // API USED BY OTHER SYSTEMS
    // =====================

    public void SetSpeedMultiplier(float value)
    {
        speedMultiplier = value;
    }

    public void ResetSpeed()
    {
        speedMultiplier = 1f;
    }

    /// <summary>
    /// Temporarily disables movement (used by Knockback / CC)
    /// </summary>
    public void Stun(float duration)
    {
        if (stunRoutine != null)
            StopCoroutine(stunRoutine);

        stunRoutine = StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        stunned = true;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(duration);
        stunned = false;
    }

    /// <summary>
    /// Forces enemy to aggressively chase the player
    /// (used by Elite death, shrine effects, taunts)
    /// </summary>
    public void ForceAggro(float duration)
    {
        if (aggroRoutine != null)
            StopCoroutine(aggroRoutine);

        aggroRoutine = StartCoroutine(ForceAggroRoutine(duration));
    }

    IEnumerator ForceAggroRoutine(float duration)
    {
        float originalMultiplier = speedMultiplier;
        speedMultiplier = Mathf.Max(speedMultiplier, 1.5f);

        yield return new WaitForSeconds(duration);

        speedMultiplier = originalMultiplier;
    }
}
