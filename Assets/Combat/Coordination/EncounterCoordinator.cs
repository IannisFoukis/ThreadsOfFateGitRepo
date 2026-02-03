using UnityEngine;
using System.Collections.Generic;

public class EncounterCoordinator : MonoBehaviour
{
    public Transform playerTransform;

    [SerializeField] private FormationResolver formationResolver;
    [SerializeField] private TacticalAuthority tacticalAuthority;

    // ─────────────────────────────
    // STATE
    // ─────────────────────────────
    public enum PhalanxState
    {
        March,
        HoldFire,
        Encircle,
        BreakChase,
        Collapse
    }

    public PhalanxState phalanxState = PhalanxState.March;
    private PhalanxState lastState;

    // ─────────────────────────────
    // STATE PERMISSIONS (SET BY ROOM CONFIG)
    // ─────────────────────────────
    [Header("Allowed Phalanx Transitions")]
    public bool allowMarch = true;
    public bool allowHoldFire = true;
    public bool allowEncircle = true;
    public bool allowBreakChase = false;
    public bool allowCollapse = false;

    // ─────────────────────────────
    // COMPRESSION / PRESSURE
    // ─────────────────────────────
    [Header("Compression Thresholds")]
    public float holdCompression = 0.7f;
    public float encircleCompression = 0.45f;

    [Header("Encircle")]
    public float encircleRadius = 4f;
    public float encircleDuration = 3.5f;

    private float stateTimer;
    private Vector3 encircleCenter;

    // ─────────────────────────────
    // AGENTS
    // ─────────────────────────────
    private readonly List<EnemyAgent> agents = new();
    private EnemyAgent formationLeader;
    private EnemyAgent currentHammer;
    private bool leaderDead;

    // ─────────────────────────────
    // DOCTRINE / LEGACY FLAGS
    // ─────────────────────────────
    private DoctrineState doctrine;

    // Silence is still queried by ranged/offender logic
    public bool SilenceActive => false;

    // ─────────────────────────────
    // DEBUG
    // ─────────────────────────────
    private float nextDebugTime;
    private const float DEBUG_INTERVAL = 0.25f;

    // ─────────────────────────────
    // UNITY
    // ─────────────────────────────
    void Awake()
    {
        if (formationResolver == null)
            formationResolver = GetComponent<FormationResolver>();

        if (tacticalAuthority == null)
            tacticalAuthority = GetComponent<TacticalAuthority>();
    }

    void Start()
    {
        ResolvePlayer();
        SetState(PhalanxState.March);
    }

    void Update()
    {
        if (agents.Count == 0 || playerTransform == null)
            return;

        ResolveLeader();

        if (stateTimer > 0f)
            stateTimer -= Time.deltaTime;

        float compression = GetFormationCompression();

        if (Time.time >= nextDebugTime)
        {
            Debug.Log(
                $"[FORMATION:{formationResolver.Current}] " +
                $"motion={phalanxState} | " +
                $"compression={compression:F2} | " +
                $"agents={agents.Count} | " +
                $"TAL={tacticalAuthority.CurrentLevel}"
            );
            nextDebugTime = Time.time + DEBUG_INTERVAL;
        }

        switch (phalanxState)
        {
            case PhalanxState.March:
                if (allowEncircle && compression <= encircleCompression)
                    EnterEncircle();
                else if (allowHoldFire && compression <= holdCompression)
                    SetState(PhalanxState.HoldFire);
                break;

            case PhalanxState.HoldFire:
                if (allowEncircle && compression <= encircleCompression)
                    EnterEncircle();
                else if (allowMarch && compression > holdCompression * 1.25f)
                    SetState(PhalanxState.March);
                break;

            case PhalanxState.Encircle:
                if (stateTimer <= 0f && allowHoldFire)
                    SetState(PhalanxState.HoldFire);
                break;

            case PhalanxState.BreakChase:
                break;

            case PhalanxState.Collapse:
                break;
        }
    }

    // ─────────────────────────────
    // STATE CONTROL
    // ─────────────────────────────
    void SetState(PhalanxState next)
    {
        if (phalanxState == next)
            return;

        lastState = phalanxState;
        phalanxState = next;
    }

    void EnterEncircle()
    {
        if (!allowEncircle)
            return;

        SetState(PhalanxState.Encircle);
        encircleCenter = playerTransform.position;
        stateTimer = encircleDuration;
        PickHammer();
    }

    // ─────────────────────────────
    // REGISTRATION
    // ─────────────────────────────
    public void Register(EnemyAgent agent)
    {
        if (!agents.Contains(agent))
            agents.Add(agent);
    }

    public void Unregister(EnemyAgent agent)
    {
        agents.Remove(agent);

        if (agent == formationLeader)
            leaderDead = true;
    }

    // ─────────────────────────────
    // LEGACY / COMPATIBILITY API
    // ─────────────────────────────

    public List<EnemyAgent> GetEnemies()
    {
        return new List<EnemyAgent>(agents);
    }

    public bool IsLeaderDead()
    {
        return leaderDead;
    }

    public bool IsHammer(EnemyAgent agent)
    {
        return agent != null && agent == currentHammer;
    }

    public void ApplyDoctrine(DoctrineState state)
    {
        doctrine = state;
    }

    // ─────────────────────────────
    // HELPERS
    // ─────────────────────────────
    void ResolveLeader()
    {
        if (formationLeader != null || leaderDead)
            return;

        formationLeader = agents.Find(a => a.role == EnemyRole.Offender);
        if (formationLeader == null && agents.Count > 0)
            formationLeader = agents[0];
    }

    float GetFormationCompression()
    {
        float sum = 0f;
        foreach (var a in agents)
        {
            if (a == null) continue;
            sum += Vector3.Distance(a.transform.position, GetWorldPositionFor(a));
        }
        return sum / Mathf.Max(1, agents.Count);
    }

    void ResolvePlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
            playerTransform = go.transform;
    }

    public Vector3 GetWorldPositionFor(EnemyAgent agent)
    {
        return phalanxState switch
        {
            PhalanxState.March => GetMarchPosition(agent),
            PhalanxState.Encircle => GetEncirclePosition(agent),
            _ => agent.transform.position
        };
    }

    Vector3 GetMarchPosition(EnemyAgent agent)
    {
        Vector3 avg = GetAverageEnemyPosition();
        Vector3 toPlayer = (playerTransform.position - avg).normalized;

        Vector3 anchor = playerTransform.position - toPlayer * 2.5f;
        Vector3 forward = (playerTransform.position - anchor).normalized;
        Vector3 right = new Vector3(-forward.y, forward.x, 0f);

        int index = agents.IndexOf(agent);

        float depth = agent.role switch
        {
            EnemyRole.Offender => 1.2f,
            EnemyRole.Defender => 2.4f,
            EnemyRole.Ranger => 3.8f,
            _ => 2.5f
        };

        float lateral = ((index % 3) - 1) * 0.8f;
        return anchor - forward * depth + right * lateral;
    }

    Vector3 GetEncirclePosition(EnemyAgent agent)
    {
        int index = agents.IndexOf(agent);
        float angle = (360f / Mathf.Max(1, agents.Count)) * index;
        return encircleCenter + Quaternion.Euler(0, 0, angle) * Vector3.up * encircleRadius;
    }

    Vector3 GetAverageEnemyPosition()
    {
        Vector3 sum = Vector3.zero;
        foreach (var a in agents)
        {
            if (a == null) continue;
            sum += a.transform.position;
        }
        return sum / Mathf.Max(1, agents.Count);
    }

    void PickHammer()
    {
        var offenders = agents.FindAll(a => a.role == EnemyRole.Offender);
        if (offenders.Count > 0)
            currentHammer = offenders[Random.Range(0, offenders.Count)];
    }
}