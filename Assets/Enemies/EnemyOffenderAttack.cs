using UnityEngine;
using System.Collections;

public class EnemyOffenderAttack : MonoBehaviour
{
    private PlayerBehaviorTracker playerBehavior;
    private PlayerController playerController;

    [Header("Deadlock Breaker")]
    public float forceCommitDistance = 1.8f;

    [Header("Commit Gate")]
    public bool enableCommitGate = true;
    public float commitDelayMin = 0.15f;
    public float commitDelayMax = 0.35f;

    [Header("Offender Intent")]
    public float offenderIntentDuration = 1.0f;

    [Header("Attack")]
    public float windUpTime = 0.6f;
    public float attackDashForce = 14f;
    public float dashDuration = 0.18f;
    public float recoverTime = 0.8f;

    [Header("Visual WindUp")]
    public float windUpCircleMaxScale = 1.2f;

    [Header("Stability")]
    public float groupReadyGrace = 0.15f;

    private EnemyAgent agent;
    private Transform player;

    private bool isWindingUp;
    private bool isRecovering;
    private bool windupCloseRangeCommit;
    private float windUpTimer;

    private float offenderIntentUntil = -1f;
    private float lastGroupReadyTime = -999f;

    private bool isCommitDelaying;
    private float commitAtTime = -1f;

    private Coroutine attackRoutine;
    private GameObject windUpCircle;

    void Awake()
    {
        playerBehavior = FindFirstObjectByType<PlayerBehaviorTracker>();
        playerController = FindFirstObjectByType<PlayerController>();
        agent = GetComponent<EnemyAgent>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        CreateWindUpCircle();
        HideCircle();
    }

    void Update()
    {
        if (agent == null || player == null || isRecovering)
            return;

        // Intent latch
        if (Time.time > offenderIntentUntil)
            offenderIntentUntil = Time.time + offenderIntentDuration;

        if (Time.time > offenderIntentUntil)
        {
            CancelWindUp();
            return;
        }

        float playerDist = Vector2.Distance(transform.position, player.position);

        bool closeRangeCommit = playerDist <= forceCommitDistance;
        bool groupReady = GroupReadyToStrike(playerDist);

        if (groupReady)
            lastGroupReadyTime = Time.time;

        // ─────────────────────────────
        // START WINDUP
        // ─────────────────────────────
        if (!isWindingUp)
        {
            if (closeRangeCommit)
                StartWindUp(true);
            else if (groupReady)
                StartWindUp(false);
        }

        // ─────────────────────────────
        // STABILITY (no flicker)
        // ─────────────────────────────
        if (isWindingUp && !windupCloseRangeCommit)
        {
            if (!groupReady && Time.time - lastGroupReadyTime > groupReadyGrace)
            {
                CancelWindUp();
                return;
            }
        }

        if (!isWindingUp)
            return;

        windUpTimer += Time.deltaTime;
        UpdateCircleVisual();

        if (windUpTimer < windUpTime)
            return;

        // ─────────────────────────────
        // COMMIT (NO BACKING OUT)
        // ─────────────────────────────
        if (enableCommitGate && !windupCloseRangeCommit)
        {
            if (!isCommitDelaying)
            {
                isCommitDelaying = true;
                commitAtTime = Time.time + Random.Range(commitDelayMin, commitDelayMax);
                return;
            }

            if (Time.time < commitAtTime)
                return;
        }

        BeginAttack();
    }

    bool GroupReadyToStrike(float playerDist)
    {
        if (agent.coordinator == null)
            return true;

        if (agent.coordinator.phalanxState == EncounterCoordinator.PhalanxState.Assemble)
            return false;

        if (agent.IsChangingFormation())
            return false;

        if (agent.coordinator.phalanxState == EncounterCoordinator.PhalanxState.March)
            return playerDist <= forceCommitDistance * 2.2f;

        return true;
    }

    void StartWindUp(bool closeRange)
    {
        isWindingUp = true;
        windupCloseRangeCommit = closeRange;
        windUpTimer = 0f;

        agent.movementLocked = true;
        isCommitDelaying = false;

        ShowCircle();
    }

    void CancelWindUp()
    {
        isWindingUp = false;
        windupCloseRangeCommit = false;

        agent.movementLocked = false;
        isCommitDelaying = false;

        HideCircle();
    }

    void BeginAttack()
    {
        isWindingUp = false;
        windupCloseRangeCommit = false;

        HideCircle();

        agent.attackLock = true;
        isRecovering = true;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector2.right;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(AttackSequence(dir));
    }

    IEnumerator AttackSequence(Vector2 dir)
    {
        yield return Dash(dir, attackDashForce, dashDuration);

        if (recoverTime > 0f)
            yield return new WaitForSeconds(recoverTime);

        agent.attackLock = false;
        isRecovering = false;
        attackRoutine = null;
        agent.SetAttackPositionLocked(false);
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

    // ───────── VISUALS ─────────

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
        float t = Mathf.Clamp01(windUpTimer / windUpTime);
        float scale = Mathf.Lerp(windUpCircleMaxScale, 0.1f, t);
        windUpCircle.transform.localScale = Vector3.one * scale;
    }

    void ShowCircle() => windUpCircle?.SetActive(true);
    void HideCircle() => windUpCircle?.SetActive(false);

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
        return Sprite.Create(tex, new Rect(0, 0, 64, 64),
            new Vector2(0.5f, 0.5f), 64);
    }
}
