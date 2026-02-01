using UnityEngine;
using System.Collections.Generic;

public class EncounterCoordinator : MonoBehaviour
{
    public Transform playerTransform;
    [SerializeField] private FormationResolver formationResolver;
    [SerializeField] private TacticalAuthority tacticalAuthority;
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
    private const float DEBUG_INTERVAL = 0.25f;

    private void Awake()
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

        // ───── Debug (semantic, throttled) ─────
        if (Time.time >= nextDebugTime)
        {
            string formation = formationResolver != null
                ? formationResolver.Current.ToString()
                : "None";

            string tal = tacticalAuthority != null
                ? tacticalAuthority.CurrentLevel.ToString()
                : "None";

            Debug.Log(
                $"[FORMATION:{formation}] " +
                $"motion={phalanxState} | " +
                $"compression={compression:F2} | " +
                $"agents={agents.Count} | " +
                $"TAL={tal}"
            );

            nextDebugTime = Time.time + DEBUG_INTERVAL;
        }

        switch (phalanxState)
        {
            case PhalanxState.March:
                if (compression <= encircleCompression &&
                    tacticalAuthority.Allows(TacticalLevel.Positional))
                {
                    EnterEncircle();
                }
                else if (compression <= holdCompression &&
                         tacticalAuthority.Allows(TacticalLevel.Positional))
                {
                    SetState(PhalanxState.HoldFire);
                }
                break;

            case PhalanxState.HoldFire:
                if (compression <= encircleCompression &&
                    tacticalAuthority.Allows(TacticalLevel.Positional))
                {
                    EnterEncircle();
                }
                else if (compression > holdCompression * 1.25f)
                {
                    SetState(PhalanxState.March);
                }
                break;

            case PhalanxState.Encircle:
                if (stateTimer <= 0f)
                    SetState(PhalanxState.HoldFire);
                break;

            case PhalanxState.BreakChase:
                // Phase M: reserved
                break;

            case PhalanxState.Collapse:
                // Chaos
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
                SpeechBus.Emit(
                    doctrine.fanatic
                        ? EnemySpeechEvent.FanaticLock
                        : EnemySpeechEvent.FormationBreak
                );
                break;

            case PhalanxState.BreakChase:
                if (doctrine.canRetreat &&
                    tacticalAuthority.Allows(TacticalLevel.Formation))
                {
                    SpeechBus.Emit(EnemySpeechEvent.RetreatCall);
                }
                break;
        }
    }

    void EnterEncircle()
    {
        SetState(PhalanxState.Encircle);
        encircleCenter = playerTransform.position;
        stateTimer = encircleDuration;

        if (tacticalAuthority.Allows(TacticalLevel.Coordinated))
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
            $"[EncounterCoordinator] Doctrine applied | " +
            $"Retreat={state.canRetreat} Chaos={state.chaotic} " +
            $"Discipline={state.formationDiscipline:0.00}"
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

            if (doctrine != null &&
                doctrine.canRetreat &&
                tacticalAuthority.Allows(TacticalLevel.Formation))
            {
                SetState(PhalanxState.BreakChase);
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

    // ───────── Compression ─────────
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

    // ───────── Position API ─────────
    public Vector3 GetWorldPositionFor(EnemyAgent agent)
    {
        if (!tacticalAuthority.Allows(TacticalLevel.Positional))
            return agent.transform.position;

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