using UnityEngine;
using System.Collections.Generic;

public class EncounterCoordinator : MonoBehaviour
{
    public Transform playerTransform;

    [SerializeField] private FormationResolver formationResolver;
    [SerializeField] private TacticalAuthority tacticalAuthority;

    [Header("Lane Logic")]
    [SerializeField] private LaneDirector laneDirector;

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
    // FORMATION FREEZE (Phase A)
    // ─────────────────────────────
    [Header("Phase A - Formation Freeze")]
    [Tooltip("When any agent gets within this distance to player, we freeze the formation reference so slots stop sliding.")]
    public float freezeDistanceToPlayer = 1.6f;

    [Tooltip("Unfreeze when all agents are farther than this distance from player.")]
    public float unfreezeDistanceToPlayer = 2.3f;

    private bool formationFrozen;
    private Vector3 frozenAvg;
    private Vector3 frozenToPlayerDir; // normalized

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

        // ───────── Freeze / Unfreeze evaluation ─────────
        float minDist = float.MaxValue;
        float maxDist = 0f;

        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];
            if (a == null) continue;

            float d = Vector2.Distance(a.transform.position, playerTransform.position);
            if (d < minDist) minDist = d;
            if (d > maxDist) maxDist = d;
        }

        // Freeze: once close, lock the FORMATION REFERENCE (avg + direction)
        if (!formationFrozen && minDist < freezeDistanceToPlayer)
        {
            formationFrozen = true;
            frozenAvg = GetAverageEnemyPosition(); // already ignores Rangers per Phase A rule

            Vector3 dir = (playerTransform.position - frozenAvg);
            frozenToPlayerDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up;
        }

        // Unfreeze: when everyone backs off enough (prevents permanent lock)
        if (formationFrozen && maxDist > unfreezeDistanceToPlayer)
        {
            formationFrozen = false;
        }

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
                $"TAL={tacticalAuthority.CurrentLevel} | " +
                $"frozen={formationFrozen}"
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

        // state change invalidates the freeze reference
        formationFrozen = false;
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

        // 🔒 Assign lane ONCE
        if (laneDirector != null)
        {
            Lane lane = laneDirector.ChooseLaneForRole(agent.role);
            agent.AssignLane(lane);
        }
    }

    public void Unregister(EnemyAgent agent)
    {
        agents.Remove(agent);

        if (agent == formationLeader)
            leaderDead = true;
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

    public bool AnyRoleChangingFormation(EnemyRole role, float threshold = -1f)
    {
        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];
            if (a == null) continue;
            if (a.role != role) continue;

            if (a.IsChangingFormation(threshold))
                return true;
        }
        return false;
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
        if (agent.attackPositionLocked || agent.attackLock)
            return agent.transform.position;

        return phalanxState switch
        {
            PhalanxState.March => GetMarchPosition(agent),
            PhalanxState.Encircle => GetEncirclePosition(agent),
            _ => agent.transform.position
        };
    }

    // ─────────────────────────────
    // FORMATION RESOLUTION (LANE-AWARE)
    // ─────────────────────────────
    Vector3 GetMarchPosition(EnemyAgent agent)
    {
        // Normal: compute avg and direction live
        Vector3 avg = GetAverageEnemyPosition();
        Vector3 toPlayer = (playerTransform.position - avg).normalized;

        // Freeze: use cached avg + direction so anchor stops sliding
        if (formationFrozen)
        {
            avg = frozenAvg;
            toPlayer = frozenToPlayerDir;
        }

        Vector3 anchor = playerTransform.position - toPlayer * 2.5f;
        Vector3 forward = (playerTransform.position - anchor).normalized;
        Vector3 right = new Vector3(-forward.y, forward.x, 0f);

        int index = agents.IndexOf(agent);

        // 🔒 PHASE A ROLE DEPTH AUTHORITY
        float depth = agent.role switch
        {
            EnemyRole.Offender => 1.2f, // always front
            EnemyRole.Defender => 2.4f, // mid / guard
            EnemyRole.Ranger => 4.2f,   // ALWAYS rear
            _ => 2.8f
        };

        float lateral = ((index % 3) - 1) * 0.8f;

        return anchor - forward * depth + right * lateral;
    }

    Vector3 GetEncirclePosition(EnemyAgent agent)
    {
        int index = agents.IndexOf(agent);
        float angle = (360f / Mathf.Max(1, agents.Count)) * index;

        float radius = agent.Lane switch
        {
            Lane.Front => encircleRadius * 0.85f,
            Lane.Flank => encircleRadius,
            Lane.Rear => encircleRadius * 1.15f,
            _ => encircleRadius
        };

        return encircleCenter + Quaternion.Euler(0, 0, angle) * Vector3.up * radius;
    }

    Vector3 GetAverageEnemyPosition()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        // Phase A rule:
        // Rangers are formation followers, not formation drivers
        foreach (var a in agents)
        {
            if (a == null) continue;
            if (a.role == EnemyRole.Ranger) continue;

            sum += a.transform.position;
            count++;
        }

        // Fallback: if only rangers exist, use everyone
        if (count == 0)
        {
            foreach (var a in agents)
            {
                if (a == null) continue;
                sum += a.transform.position;
                count++;
            }
        }

        return count > 0 ? sum / count : transform.position;
    }

    void PickHammer()
    {
        var offenders = agents.FindAll(a => a.role == EnemyRole.Offender);
        if (offenders.Count > 0)
            currentHammer = offenders[Random.Range(0, offenders.Count)];
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
}