using UnityEngine;
using System.Collections.Generic;
using TOF.Rooms.Contracts;

public class EncounterCoordinator : MonoBehaviour
{

    private bool silenceActive;

    public Transform playerTransform;
    public RoomDoctrineConfig doctrine;
    private EnemyAgent formationLeader;
    private bool honestAttacks;

    private readonly List<EnemyAgent> agents = new List<EnemyAgent>();

    [Header("Leader Death / Coordination Decay")]
    public float leaderDeathHesitation = 0.6f;
    public float coordinationDecayTime = 10f;
    public float maxSlotJitter = 0.6f;
    public float maxRotationWobble = 18f;
    public float hammerIntervalDeadMultiplier = 0.65f;

    [Header("Leaderless Drift")]
    public float minDriftStrength = 0.15f;
    public float maxDriftStrength = 0.45f;

    private bool leaderDead = false;
    private float leaderDiedAt = -1f;
    private float cohesion01 = 1f;
    private Vector3 leaderAnchorPos;

    private float lastLeaderAngle = 0f;
    private bool rotationLocked = false;
    private Vector3 encircleCenter;

    [Header("Arcade Phalanx Radii")]
    public float holdRadius = 12f;
    public float encircleRadius = 6f;

    [Header("Phase Timers")]
    public float encircleDuration = 3.5f;
    public float breakChaseDuration = 2.0f;

    [Header("Encircle Geometry")]
    public float encircleRadiusOffenders = 5f;
    public float encircleRadiusDefenders = 7f;
    public float encircleRadiusRangers = 9f;

    [Header("Hammer Logic")]
    public float hammerInterval = 2.2f;
    public float hammerWindow = 0.8f;

    private float stateTimer = 0f;
    private float hammerTimer = 0f;
    private EnemyAgent currentHammer;
    private float hammerUntil = 0f;

    [Header("Spawn Grace")]
    public float stateGraceTime = 0.3f;
    private float encounterStartTime = -1f;

    [Header("Chase Delay")]
    public float chaseDelay = 2f;
    private float chaseDelayTimer = 0f;

    [Header("Relative Speeds")]
    public float breakChaseSpeedMultiplier = 0.25f;

    [Header("Formation Smoothing")]
    public float globalFormationLerp = 6f;

    [Header("Formation Spacing")]
    public float lineSpacing = 1.8f;
    public float wedgeSpacing = 1.6f;
    public float boxSpacing = 2.4f;
    public float diamondSpacing = 2.8f;

    public enum PhalanxState { March, HoldFire, Encircle, BreakChase, Collapse }
    public enum FormationType { Line, Wedge, Box, Diamond }

    public FormationType activeFormation = FormationType.Line;
    public PhalanxState phalanxState = PhalanxState.March;

    // ─────────────────────────────────────────────
    // ROOM CONTRACT FLAGS (DATA ONLY – NO LOGIC YET)
    // ─────────────────────────────────────────────

    [Header("Room Contract Flags")]
    public bool allowHammer = true;
    public bool allowEncircle = true;
    public bool allowElites = true;

    [Header("Room Contract Multipliers")]
    public float hammerAggressionMultiplier = 1f;
    public float encircleSpeedMultiplier = 1f;
    public bool SilenceActive => silenceActive;
    public bool silencePhase = false;

    [Header("Room Contract Variant Chances")]
    [Range(0f, 1f)] public float fakeOutChance = 0f;
    [Range(0f, 1f)] public float delayedDashChance = 0f;

    void Start()
    {
        if (RunContext.Instance != null)
        {
            // Silence from memory
            if (RunContext.Instance.memory != null)
                silenceActive = RunContext.Instance.memory.silenceActive;

            // 👇 NEW: Honest attack bias from Keeper
            honestAttacks = RunContext.Instance.worldModifiers != null &&
                            RunContext.Instance.worldModifiers.enemiesFavorHonestAttacks;
        }

        // Silence overrides everything
        if (silenceActive)
        {
            silencePhase = true;
            allowHammer = false;
            allowEncircle = false;

            fakeOutChance = 0f;
            delayedDashChance = 0f;

            hammerAggressionMultiplier = 0.7f;
            encircleSpeedMultiplier = 0.6f;
        }

        // 👇 Keeper judgment: honest combat (no tricks)
        if (honestAttacks)
        {
            fakeOutChance = 0f;
            delayedDashChance = 0f;

            Debug.Log("[EncounterCoordinator] Honest attacks enforced (Keeper judgment)");
        }

        Debug.Log($"[EncounterCoordinator] Silence={silenceActive} Honest={honestAttacks}");

        ResolvePlayer();
        SetState(PhalanxState.March);
    }


    void Update()
    {
        var contract = RoomAccess.Current;
        if (contract != null && contract.silencePhase)
        {
            silencePhase = true;
        }
        if (contract == null)
            return;

        bool shouldRun = contract.useCoordinator && !contract.silencePhase;
        if (!shouldRun)
        {
            if (enabled)
                Debug.Log("[Coordinator] Disabled by contract");
            enabled = false;
            return;
        }

        enabled = true;

        if (agents.Count == 0) return;

        ResolveLeader();
        if (playerTransform == null) ResolvePlayer();
        if (playerTransform == null) return;

        if (leaderDead)
        {
            float t = (Time.time - leaderDiedAt) / Mathf.Max(0.01f, coordinationDecayTime);
            cohesion01 = Mathf.Clamp01(1f - t);
        }
        else cohesion01 = 1f;

        Vector3 leaderPos = formationLeader != null ? formationLeader.transform.position : leaderAnchorPos;
        float dist = Vector3.Distance(leaderPos, playerTransform.position);

        if (stateTimer > 0f) stateTimer -= Time.deltaTime;

        switch (phalanxState)
        {
            case PhalanxState.March:
                rotationLocked = false;
                if (dist <= encircleRadius) EnterEncircle();
                else if (dist <= holdRadius) EnterHoldFire();
                break;

            case PhalanxState.HoldFire:
                rotationLocked = true;
                if (dist <= encircleRadius) EnterEncircle();
                else if (dist > holdRadius * 1.1f) EnterBreakChase();
                break;

            case PhalanxState.Encircle:
                rotationLocked = true;
                UpdateHammerLogic();
                if (dist > holdRadius * 1.1f) EnterBreakChase();
                else if (stateTimer <= 0f) EnterHoldFire();
                break;

            case PhalanxState.BreakChase:
                rotationLocked = false;
                if (chaseDelayTimer > 0f)
                {
                    chaseDelayTimer -= Time.deltaTime;
                    break;
                }
                if (stateTimer <= 0f) SetState(PhalanxState.March);
                break;
        }
    }
    public void ApplyRoomContract(RoomContract contract)
    {
        Debug.Log($"[EncounterCoordinator] Applying contract: {contract.contractName}");

        allowHammer = contract.allowHammer;
        allowEncircle = contract.allowEncircle;

        hammerAggressionMultiplier = contract.hammerAggression;
        encircleSpeedMultiplier = contract.encircleSpeedMultiplier;

        silencePhase = contract.silencePhase;

        fakeOutChance = contract.fakeOutChance;
        delayedDashChance = contract.delayedDashChance;

        allowElites = contract.allowElites;

        if (contract.reduceAudio)
        {
            // Hook later to audio system
            Debug.Log("[EncounterCoordinator] Audio dampened (Silence Phase)");
        }
    }

    // =========================================================
    // WORLD POSITION (FINAL, LOCKED)
    // =========================================================

    public Vector3 GetWorldPositionFor(EnemyAgent agent)
    {
        if (playerTransform == null)
            return agent.transform.position;

        if (leaderDead)
        {
            Vector3 pos = (phalanxState == PhalanxState.Encircle)
                ? GetEncirclePosition(agent)
                : GetPhalanxPositionAnchored(agent);

            // 🔻 LEADERLESS DRIFT (THE KEY)
            float drift = Mathf.Lerp(minDriftStrength, maxDriftStrength, 1f - cohesion01);
            pos += (playerTransform.position - leaderAnchorPos) * drift;

            return pos;
        }

        if (formationLeader == null)
            return agent.transform.position;

        if (phalanxState == PhalanxState.Encircle)
            return GetEncirclePosition(agent);

        if (phalanxState == PhalanxState.BreakChase)
            return playerTransform.position;

        return GetPhalanxPosition(agent);
    }

    // =========================================================
    // FORMATION POSITIONS
    // =========================================================

    Vector3 GetPhalanxPositionAnchored(EnemyAgent agent)
    {
        float angle = GetLeaderRotation();
        Quaternion rot = Quaternion.Euler(0, 0, angle);
        Vector3 forward = rot * Vector3.up;
        Vector3 right = rot * Vector3.right;

        int index = agents.IndexOf(agent);
        Vector3 pos;

        switch (activeFormation)
        {
            case FormationType.Line:
                pos = leaderAnchorPos + forward * doctrine.offenderRadius +
                      right * ((index - agents.Count / 2f) * lineSpacing);
                break;

            case FormationType.Box:
                int size = Mathf.CeilToInt(Mathf.Sqrt(agents.Count));
                int row = index / size;
                int col = index % size;
                pos = leaderAnchorPos + forward * (row * boxSpacing) +
                      right * ((col - (size - 1) / 2f) * boxSpacing);
                break;

            case FormationType.Wedge:
                int r = index / 2;
                int s = (index % 2 == 0) ? -1 : 1;
                pos = leaderAnchorPos + forward * (r * wedgeSpacing) +
                      right * (r * wedgeSpacing * s);
                break;

            default:
                int layer = index / 4 + 1;
                int p = index % 4;
                pos = leaderAnchorPos + (p switch
                {
                    0 => forward,
                    1 => right,
                    2 => -forward,
                    _ => -right
                }) * diamondSpacing * layer;
                break;
        }

        float j = Mathf.Lerp(0f, maxSlotJitter, 1f - cohesion01);
        return pos + new Vector3(Random.Range(-j, j), Random.Range(-j, j), 0f);
    }

    Vector3 GetPhalanxPosition(EnemyAgent agent)
    {
        return GetPhalanxPositionAnchored(agent);
    }

    Vector3 GetEncirclePosition(EnemyAgent agent)
    {
        float radius = agent.role switch
        {
            EnemyRole.Offender => encircleRadiusOffenders,
            EnemyRole.Defender => encircleRadiusDefenders,
            EnemyRole.Ranger => encircleRadiusRangers,
            _ => encircleRadiusDefenders
        };

        int index = agents.IndexOf(agent);
        float angle = (360f / Mathf.Max(1, agents.Count)) * index;
        return encircleCenter + Quaternion.Euler(0, 0, angle) * Vector3.up * radius;
    }

    float GetLeaderRotation()
    {
        Vector3 leaderPos = formationLeader != null ? formationLeader.transform.position : leaderAnchorPos;
        Vector3 toPlayer = playerTransform.position - leaderPos;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;

        if (!leaderDead)
        {
            lastLeaderAngle = baseAngle;
            return baseAngle;
        }

        float wobble = Mathf.Sin(Time.time * 3.2f) *
                       Mathf.Lerp(0f, maxRotationWobble, 1f - cohesion01);

        lastLeaderAngle = baseAngle + wobble;
        return lastLeaderAngle;
    }

    // =========================================================
    // LEADER + HAMMER
    // =========================================================

    void ResolveLeader()
    {
        if (formationLeader != null)
        {
            leaderAnchorPos = formationLeader.transform.position;
            return;
        }

        if (leaderDead) return;

        formationLeader = agents.Find(a => a.role == EnemyRole.Offender);
        if (formationLeader == null && agents.Count > 0)
            formationLeader = agents[0];

        if (formationLeader != null)
            leaderAnchorPos = formationLeader.transform.position;
    }

    void UpdateHammerLogic()
    {
        if (!allowHammer || silencePhase)
            return;

        hammerTimer -= Time.deltaTime;
        if (hammerTimer <= 0f)
        {
            PickNewHammer();
            hammerTimer = hammerInterval * hammerAggressionMultiplier;
        }
    }


    void PickNewHammer()
    {
        var offenders = agents.FindAll(a => a.role == EnemyRole.Offender);
        if (offenders.Count == 0) return;

        currentHammer = offenders[Random.Range(0, offenders.Count)];
        float window = leaderDead ? hammerWindow * hammerIntervalDeadMultiplier : hammerWindow;
        hammerUntil = Time.time + window;
    }

    // =========================================================
    // STATE TRANSITIONS
    // =========================================================

    void EnterHoldFire() => SetState(PhalanxState.HoldFire);

    void EnterEncircle()
    {
        if (!allowEncircle || silencePhase)
            return;

        SetState(PhalanxState.Encircle);
        encircleCenter = playerTransform.position;
        stateTimer = encircleDuration * encircleSpeedMultiplier;
        hammerTimer = hammerInterval;
    }


    void EnterBreakChase()
    {
        SetState(PhalanxState.BreakChase);
        stateTimer = breakChaseDuration;
        chaseDelayTimer = chaseDelay;
    }

    void SetState(PhalanxState s) => phalanxState = s;

    // =========================================================
    // REG / UNREG
    // =========================================================

    public void Register(EnemyAgent agent)
    {
        if (!agents.Contains(agent))
        {
            agents.Add(agent);
            if (encounterStartTime < 0f) encounterStartTime = Time.time;
        }
    }

    public void Unregister(EnemyAgent agent)
    {
        agents.Remove(agent);
        if (agent == formationLeader)
        {
            leaderDead = true;
            leaderDiedAt = Time.time;
            leaderAnchorPos = agent.transform.position;
            Debug.Log("[PHALANX] LEADER DOWN");
        }
    }
    // =========================================================
    // PUBLIC API (EXPECTED BY EnemyAgent / DEBUG / GIZMOS)
    // =========================================================

    public bool IsLeader(EnemyAgent agent)
    {
        return agent != null && agent == formationLeader;
    }

    public List<EnemyAgent> GetEnemies()
    {
        return new List<EnemyAgent>(agents);
    }

    void ResolvePlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) playerTransform = go.transform;
    }

    public bool IsHammer(EnemyAgent a) => a == currentHammer;
    public bool IsLeaderDead() => leaderDead;
}
