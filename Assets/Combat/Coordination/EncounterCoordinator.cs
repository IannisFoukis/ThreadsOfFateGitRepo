using UnityEngine;
using System.Collections.Generic;

public class EncounterCoordinator : MonoBehaviour
{
    public Transform playerTransform;

    // ───────── Doctrine (Injected by RoomDirector) ─────────
    private DoctrineState doctrine;

    // ───────── Legacy compatibility ─────────
    public float globalFormationLerp = 6f;
    public bool SilenceActive => false;

    private readonly List<EnemyAgent> agents = new();
    private EnemyAgent formationLeader;
    private EnemyAgent currentHammer;
    private bool leaderDead;

    // ───────── States ─────────
    public enum PhalanxState
    {
        March,
        HoldFire,
        Encircle,
        BreakChase,
        Collapse
    }

    public PhalanxState phalanxState = PhalanxState.March;

    [Header("Phase L – Formation Compression Thresholds")]
    [Tooltip("Average distance to slot before holding fire")]
    public float holdCompression = 0.6f;

    [Tooltip("Average distance to slot before encircle")]
    public float encircleCompression = 0.3f;

    [Header("Encircle")]
    public float encircleRadius = 4f;
    public float encircleDuration = 3.5f;

    private float stateTimer;
    private Vector3 encircleCenter;

    void Start()
    {
        ResolvePlayer();
        phalanxState = PhalanxState.March;
    }

    void Update()
    {
        if (agents.Count == 0 || playerTransform == null)
            return;

        ResolveLeader();

        // 🔥 Doctrine-driven collapse
        if (doctrine != null && doctrine.chaotic && doctrine.IsFormationBreaking())
        {
            if (phalanxState != PhalanxState.Collapse)
                EnterCollapse();
            return;
        }

        if (stateTimer > 0f)
            stateTimer -= Time.deltaTime;

        float compression = GetFormationCompression();

        switch (phalanxState)
        {
            case PhalanxState.March:
                if (compression <= encircleCompression)
                    EnterEncircle();
                else if (compression <= holdCompression)
                    phalanxState = PhalanxState.HoldFire;
                break;

            case PhalanxState.HoldFire:
                if (compression <= encircleCompression)
                    EnterEncircle();
                else if (compression > holdCompression * 1.25f)
                    phalanxState = PhalanxState.March;
                break;

            case PhalanxState.Encircle:
                if (stateTimer <= 0f)
                    phalanxState = PhalanxState.HoldFire;
                break;

            case PhalanxState.BreakChase:
                // Reserved (Phase M)
                break;

            case PhalanxState.Collapse:
                // Pure chaos – enemies act individually
                break;
        }
    }

    // ───────── Doctrine Injection (FINAL) ─────────
    public void ApplyDoctrine(DoctrineState state)
    {
        doctrine = state;

        Debug.Log(
            $"[EncounterCoordinator] Doctrine applied | " +
            $"Retreat={state.canRetreat}, Sacrifice={state.canSacrifice}, " +
            $"Chaos={state.chaotic}, Discipline={state.formationDiscipline:0.00}"
        );
    }

    // ───────── Position API ─────────
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

    void EnterEncircle()
    {
        phalanxState = PhalanxState.Encircle;
        encircleCenter = playerTransform.position;
        stateTimer = encircleDuration;
        PickHammer();

        Debug.Log("[PHALANX] ENCIRCLE");
    }

    void EnterCollapse()
    {
        phalanxState = PhalanxState.Collapse;
        Debug.Log("[PHALANX] FORMATION COLLAPSE");

        if (doctrine != null && doctrine.canSacrifice)
            Debug.Log("[PHALANX] Sacrifice behaviors permitted");
    }

    // ───────── Registration ─────────
    public void Register(EnemyAgent agent)
    {
        if (!agents.Contains(agent))
            agents.Add(agent);
    }

    public void Unregister(EnemyAgent agent)
    {
        agents.Remove(agent);

        if (agent == formationLeader)
        {
            leaderDead = true;
            Debug.Log("[PHALANX] LEADER DOWN");

            if (doctrine != null && doctrine.canRetreat)
            {
                phalanxState = PhalanxState.BreakChase;
                Debug.Log("[PHALANX] RETREAT AUTHORIZED");
            }
        }
    }

    void ResolveLeader()
    {
        if (formationLeader != null || leaderDead)
            return;

        formationLeader = agents.Find(a => a.role == EnemyRole.Offender);
        if (formationLeader == null && agents.Count > 0)
            formationLeader = agents[0];
    }

    // ───────── Helpers ─────────
    float GetFormationCompression()
    {
        float sum = 0f;
        foreach (var a in agents)
            sum += Vector3.Distance(a.transform.position, GetWorldPositionFor(a));

        return sum / Mathf.Max(1, agents.Count);
    }

    Vector3 GetAverageEnemyPosition()
    {
        Vector3 sum = Vector3.zero;
        foreach (var a in agents)
            sum += a.transform.position;

        return sum / Mathf.Max(1, agents.Count);
    }

    void ResolvePlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
            playerTransform = go.transform;
    }

    // ───────── Legacy API ─────────
    public bool IsLeaderDead() => leaderDead;
    public bool IsHammer(EnemyAgent agent) => agent == currentHammer;
    public List<EnemyAgent> GetEnemies() => new List<EnemyAgent>(agents);

    void PickHammer()
    {
        var offenders = agents.FindAll(a => a.role == EnemyRole.Offender);
        if (offenders.Count > 0)
            currentHammer = offenders[Random.Range(0, offenders.Count)];
    }
}