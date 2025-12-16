using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyCharger : MonoBehaviour
{
    public float chargeForce = 8f;
    public float chargeCooldown = 2f;

    Rigidbody2D rb;
    Transform player;
    EnemyStateController state;
    bool canCharge = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        state = GetComponent<EnemyStateController>();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (GameLock.IsLocked) return;

        if (!player || !canCharge || state.CurrentState == EnemyState.Dead)
            return;

        StartCoroutine(Charge());
    }

    IEnumerator Charge()
    {
        canCharge = false;
        state.SetState(EnemyState.Hit);

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dir * chargeForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.5f);
        state.SetState(EnemyState.Idle);

        yield return new WaitForSeconds(chargeCooldown);
        canCharge = true;
    }
}
