using UnityEngine;
using System.Collections.Generic;

public class EncounterCoordinator : MonoBehaviour
{
    public Transform playerTransform;

    private DoctrineState doctrine;

    public float globalFormationLerp = 6f;
    public bool SilenceActive => false;

    private readonly List<EnemyAgent> agents = new();
    private EnemyAgent formationLeader;
    private EnemyAgent currentHammer;
    private bool leaderDead;

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

    [Header("Phase L – Formation Compression Thresholds")]
    public float holdCompression = 0.6f;
    public float encircleCompression = 0.3f;

    [Header("Encircle")]
    public float encircleRadius = 4f;
    public float encircleDuration = 3.5f;

    private float stateTimer;
    private Vector3 encircleCenter;

    // Debug throttle
    private float nextDebugTime;

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

        // doctrine-driven collapse (optional hook)
        if (doctrine != null && doctrine.chaotic && doctrine.IsFormationBreaking())
        {
            if (phalanxState != PhalanxState.Collapse)
                EnterCollapse();
            return;
        }

        if (stateTimer > 0f)
            stateTimer -= Time.deltaTime;

        float compression = GetFormationCompression();

        if (Time.time >= nextDebugTime)
        {
            Debug.Log($"[PHALANX] compression={compression:0.00} state={phalanxState} agents={agents.Count}");
            nextDebugTime = Time.time + 0.5f;
        }

        switch (phalanxState)
        {
            case PhalanxState.March:
                if (compression <= encircleCompression)
                    EnterEncircle();
                else if (compression <= holdCompression)
                    SetState(PhalanxState.HoldFire);
                break;

            case PhalanxState.HoldFire:
                if (compression <= encircleCompression)
                    EnterEncircle();
                else if (compression > holdCompression * 1.25f)
                    SetState(PhalanxState.March);
                break;

            case PhalanxState.Encircle:
                if (stateTimer <= 0f)
                    SetState(PhalanxState.HoldFire);
                break;

            case PhalanxState.BreakChase:
                // Phase M: reserved (retreat movement logic later)
                break;

            case PhalanxState.Collapse:
                // Chaos: ignore formation targets (enemies free-act)
                break;
        }
    }

    // ───────── Phase M — State + Speech ─────────

    void SetState(PhalanxState next)
    {
        if (phalanxState == next)
            return;

        lastState = phalanxState;
        phalanxState = next;

        EmitFormationSpeech(lastState, next);
    }

    void EmitFormationSpeech(PhalanxState from, PhalanxState to)
    {
        if (doctrine == null)
            return;

        // Optional: only talk if discipline is high enough (keeps it “rare”)
        // if (doctrine.formationDiscipline < 0.5f) return;

        if (from == PhalanxState.March && to == PhalanxState.HoldFire)
            SpeechBus.Emit(EnemySpeechEvent.Advance);

        switch (to)
        {
            case PhalanxState.HoldFire:
                SpeechBus.Emit(EnemySpeechEvent.HoldLine);
                break;

            case PhalanxState.Encircle:
                SpeechBus.Emit(EnemySpeechEvent.EncircleCall);
                break;

            case PhalanxState.Collapse:
                if (doctrine.fanatic)
                    SpeechBus.Emit(EnemySpeechEvent.FanaticLock);
                else
                    SpeechBus.Emit(EnemySpeechEvent.FormationBreak);
                break;

            case PhalanxState.BreakChase:
                if (doctrine.canRetreat)
                    SpeechBus.Emit(EnemySpeechEvent.RetreatCall);
                break;
        }
    }

    void EnterEncircle()
    {
        SetState(PhalanxState.Encircle);
        encircleCenter = playerTransform.position;
        stateTimer = encircleDuration;
        PickHammer();
    }

    void EnterCollapse()
    {
        SetState(PhalanxState.Collapse);
    }

    // ───────── Doctrine Injection ─────────
    public void ApplyDoctrine(DoctrineState state)
    {
        doctrine = state;

        Debug.Log(
            $"[EncounterCoordinator] Doctrine applied | Retreat={state.canRetreat} " +
            $"Chaos={state.chaotic} Discipline={state.formationDiscipline:0.00}"
        );
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

            if (doctrine != null && doctrine.canRetreat)
                SetState(PhalanxState.BreakChase);
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

    // ───────── Compression ─────────
    float GetFormationCompression()
    {
        float sum = 0f;
        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];
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

    Vector3 GetAverageEnemyPosition()
    {
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];
            if (a == null) continue;
            sum += a.transform.position;
        }
        return sum / Mathf.Max(1, agents.Count);
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