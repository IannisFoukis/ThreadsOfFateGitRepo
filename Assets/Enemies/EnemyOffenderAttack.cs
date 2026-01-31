using UnityEngine;
using System.Collections;

public class EnemyOffenderAttack : MonoBehaviour
{
    [Header("Offender Intent")]
    public float offenderIntentDuration = 1.0f;
    private float offenderIntentUntil = -1f;

    [Header("Variants")]
    public bool canFakeOut = false;
    [Range(0.3f, 0.8f)] public float fakeCancelAt = 0.7f;
    public float fakePause = 0.35f;

    public bool delayedDash = false;
    public float postWindUpDelay = 0.2f;

    public bool doubleDash = false;
    public float secondDashDelay = 0.15f;
    public float secondDashMultiplier = 1.4f;

    [Header("Attack")]
    public float windUpTime = 0.6f;
    public float attackDashForce = 14f;
    public float dashDuration = 0.18f;
    public float recoverTime = 0.8f;

    [Header("Slot Trigger")]
    public float slotTriggerMultiplier = 1.5f;

    [Header("Visual WindUp")]
    public float windUpCircleMaxScale = 1.2f;

    private EnemyAgent agent;
    private Transform player;

    private bool isWindingUp;
    private bool isRecovering;
    private float windUpTimer;

    private bool hasFaked;
    private Coroutine attackRoutine;

    private GameObject windUpCircle;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        CreateWindUpCircle();
        HideCircle();
    }

    void OnDisable()
    {
        if (agent != null) agent.attackLock = false;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        CancelInvoke();
        CancelWindUp();
        isRecovering = false;
        hasFaked = false;
    }

    void Update()
    {
        if (agent == null || player == null)
            return;

        // Silence Phase: force honest attacks
        if (agent.coordinator != null && agent.coordinator.SilenceActive)
        {
            canFakeOut = false;
            delayedDash = false;
            doubleDash = false;
        }

        // 🔒 Latch offender intent ONCE
        if (agent.role == EnemyRole.Offender && Time.time > offenderIntentUntil)
            offenderIntentUntil = Time.time + offenderIntentDuration;

        // No intent or busy recovering
        if (Time.time > offenderIntentUntil || isRecovering)
        {
            CancelWindUp();
            return;
        }

        // Slot-based trigger (formation driven)
        Vector3 slotPos3 = agent.GetFormationTarget();
        Vector2 slotPos = new Vector2(slotPos3.x, slotPos3.y);

        float slotDist = Vector2.Distance(transform.position, slotPos);
        float slotThreshold = agent.SlotArrivalThreshold * slotTriggerMultiplier;

        if (!isWindingUp && slotDist <= slotThreshold)
            StartWindUp();

        if (isWindingUp && slotDist > slotThreshold * 1.2f)
        {
            CancelWindUp();
            return;
        }

        if (!isWindingUp)
            return;

        windUpTimer += Time.deltaTime;
        UpdateCircleVisual();

        // Fake-out
        if (canFakeOut && !hasFaked && windUpTimer >= windUpTime * fakeCancelAt)
        {
            hasFaked = true;
            CancelWindUp();
            Invoke(nameof(RestartWindUp), fakePause);
            return;
        }

        if (windUpTimer >= windUpTime)
        {
            hasFaked = false;
            BeginAttack();
        }
    }

    void RestartWindUp()
    {
        if (!isRecovering)
            StartWindUp();
    }

    void StartWindUp()
    {
        isWindingUp = true;
        windUpTimer = 0f;
        ShowCircle();
    }

    void CancelWindUp()
    {
        isWindingUp = false;
        HideCircle();
    }

    void BeginAttack()
    {
        isWindingUp = false;
        HideCircle();

        agent.attackLock = true;
        isRecovering = true;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(AttackSequence(dir));
    }

    IEnumerator AttackSequence(Vector2 dir)
    {
        if (delayedDash && postWindUpDelay > 0f)
            yield return new WaitForSeconds(postWindUpDelay);

        yield return Dash(dir, attackDashForce, dashDuration);

        if (doubleDash)
        {
            if (secondDashDelay > 0f)
                yield return new WaitForSeconds(secondDashDelay);

            yield return Dash(dir, attackDashForce * secondDashMultiplier, dashDuration);
        }

        if (recoverTime > 0f)
            yield return new WaitForSeconds(recoverTime);

        agent.attackLock = false;
        isRecovering = false;
        attackRoutine = null;
    }

    IEnumerator Dash(Vector2 dir, float speed, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }
    }

    // ───────────── VISUALS ─────────────

    void CreateWindUpCircle()
    {
        windUpCircle = new GameObject("WindUpCircle");
        windUpCircle.transform.SetParent(transform);
        windUpCircle.transform.localPosition = Vector3.zero;

        var sr = windUpCircle.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateCircleSprite();
        sr.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        sr.sortingLayerName = "Ground";
        sr.sortingOrder = 0;
    }

    void UpdateCircleVisual()
    {
        float t = Mathf.Clamp01(windUpTimer / Mathf.Max(0.0001f, windUpTime));
        float scale = Mathf.Lerp(windUpCircleMaxScale, 0.1f, t);
        windUpCircle.transform.localScale = Vector3.one * scale;
    }

    void ShowCircle() => windUpCircle.SetActive(true);
    void HideCircle() { if (windUpCircle != null) windUpCircle.SetActive(false); }

    Sprite GenerateCircleSprite()
    {
        Texture2D tex = new Texture2D(64, 64);
        Color clear = new Color(0, 0, 0, 0);

        for (int x = 0; x < 64; x++)
            for (int y = 0; y < 64; y++)
            {
                float dx = x - 32;
                float dy = y - 32;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, d <= 30 ? Color.white : clear);
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
    }
}