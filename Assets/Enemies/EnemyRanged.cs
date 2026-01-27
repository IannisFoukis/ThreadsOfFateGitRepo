using UnityEngine;

public class EnemyRanged : MonoBehaviour
{
    [Header("Firing")]
    public Projectile projectilePrefab;
    public float fireCooldown = 1.8f;
    public float fireRange = 9f;
    public float projectileSpeed = 7f;
    public float projectileLifetime = 3f;
    public int damage = 1;
    public ProjectileModifiers modifiers;

    [Header("Aim Telegraph")]
    public float aimTime = 0.6f;
    public float aimLineWidth = 0.05f;
    public Color aimColor = new Color(1f, 0.8f, 0.2f, 0.8f);

    [Header("Reposition")]
    public float fleeDistance = 2.5f;
    public float repositionCooldown = 2.0f;

    // Soft reposition (transform-driven)
    public float repositionSpeed = 8f;
    public float repositionDuration = 0.25f;

    [Header("Cadence")]
    public float pressureBonusCooldown = 0.6f;

    [Header("Silence Phase")]
    [Tooltip("Multiplier applied to fire cooldown during Silence Phase")]
    public float silenceCooldownMultiplier = 1.5f;

    float lastFireTime;
    float aimTimer;
    bool isAiming;
    float lastRepositionTime;

    bool isRepositioning;
    float repositionTimer;
    Vector2 repositionDir;

    EnemyAgent agent;
    Transform player;
    LineRenderer aimLine;

    void Awake()
    {
        agent = GetComponent<EnemyAgent>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // ===== Aim Line =====
        aimLine = gameObject.AddComponent<LineRenderer>();
        aimLine.enabled = false;
        aimLine.positionCount = 2;
        aimLine.startWidth = aimLineWidth;
        aimLine.endWidth = aimLineWidth;
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor = aimColor;
        aimLine.endColor = aimColor;
        aimLine.sortingLayerName = "Ground";
        aimLine.sortingOrder = 0;
    }

    void Update()
    {
        if (agent == null || player == null)
        {
            CancelAim();
            return;
        }

        // 🔒 Only Rangers act here
        if (agent.role != EnemyRole.Ranger)
        {
            CancelAim();
            return;
        }

        // ===== Silence Phase check =====
        bool silence =
            agent.coordinator != null &&
            agent.coordinator.SilenceActive;

        // ===== Soft reposition burst =====
        if (isRepositioning)
        {
            CancelAim();

            transform.position += (Vector3)(repositionDir * repositionSpeed * Time.deltaTime);
            repositionTimer += Time.deltaTime;

            if (repositionTimer >= repositionDuration)
            {
                isRepositioning = false;
                repositionTimer = 0f;
            }

            return;
        }

        // Reposition trigger if player too close
        float closeDist = Vector2.Distance(transform.position, player.position);
        if (closeDist < fleeDistance && Time.time > lastRepositionTime + repositionCooldown)
        {
            StartReposition();
            lastRepositionTime = Time.time;
            return;
        }

        // 🔒 Must be stable in formation
        if (!agent.IsAtSlot(2f) || agent.IsChangingFormation())
        {
            CancelAim();
            return;
        }

        // ===== Dynamic cooldown =====
        float dynamicCooldown = fireCooldown;

        if (silence)
        {
            // Silence = slower, deliberate shots
            dynamicCooldown *= silenceCooldownMultiplier;
        }
        else if (agent.coordinator != null && agent.coordinator.IsLeaderDead())
        {
            // Panic fire only when NOT silent
            dynamicCooldown = Mathf.Max(0.2f, dynamicCooldown - pressureBonusCooldown);
        }

        if (Time.time < lastFireTime + dynamicCooldown)
        {
            CancelAim();
            return;
        }

        // 🔒 Range check
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > fireRange)
        {
            CancelAim();
            return;
        }

        // ===== AIM PHASE =====
        if (!isAiming)
            StartAim();

        aimTimer += Time.deltaTime;
        UpdateAimLine();

        if (aimTimer >= aimTime)
            Fire();
    }

    // ================= REPOSITION =================

    void StartReposition()
    {
        CancelAim();
        isRepositioning = true;
        repositionTimer = 0f;

        repositionDir = ((Vector2)transform.position - (Vector2)player.position).normalized;
        if (repositionDir.sqrMagnitude < 0.0001f)
            repositionDir = Vector2.right;
    }

    // ================= AIM =================

    void StartAim()
    {
        isAiming = true;
        aimTimer = 0f;
        aimLine.enabled = true;
    }

    void CancelAim()
    {
        isAiming = false;
        aimTimer = 0f;
        if (aimLine != null)
            aimLine.enabled = false;
    }

    void UpdateAimLine()
    {
        if (!aimLine.enabled) return;

        aimLine.SetPosition(0, transform.position);
        aimLine.SetPosition(1, player.position);
    }

    // ================= FIRE =================

    void Fire()
    {
        CancelAim();
        lastFireTime = Time.time;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        proj.Fire(dir, projectileSpeed, projectileLifetime, damage, modifiers);

        // 🔓 Punish window
        enabled = false;
        Invoke(nameof(EnableAgain), 0.4f);

        // 🔴 Visual feedback
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.yellow;
            Invoke(nameof(ResetColor), 0.15f);
        }
    }

    void EnableAgain()
    {
        if (this != null)
            enabled = true;
    }

    void ResetColor()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.white;
    }
}
