using UnityEngine;
using System.Collections.Generic;

public class EncounterCoordinator : MonoBehaviour
{
    public Transform playerTransform;
    public RoomDoctrineConfig doctrine;
    private EnemyAgent formationLeader;

    private readonly List<EnemyAgent> agents = new List<EnemyAgent>();

    private float lastLeaderAngle = 0f;
    private bool rotationLocked = false;
    private Vector3 encircleCenter;

    [Header("Arcade Phalanx Radii")]
    [Tooltip("Outer range: enter HoldFire when player is within this distance of leader (unless already in Encircle).")]
    public float holdRadius = 12f;

    [Tooltip("Inner range: enter Encircle when player is within this distance of leader.")]
    public float encircleRadius = 6f;

    [Header("Phase Timers")]
    public float encircleDuration = 3.5f;
    public float breakChaseDuration = 2.0f;

    [Header("Encircle Geometry")]
    public float encircleRadiusOffenders = 5f;
    public float encircleRadiusDefenders = 7f;
    public float encircleRadiusRangers = 9f;

    [Header("Mixed Pressure (C)")]
    public float hammerInterval = 2.2f;
    public float hammerWindow = 0.8f;

    private float stateTimer = 0f;
    private float hammerTimer = 0f;
    private EnemyAgent currentHammer;
    private float hammerUntil = 0f;

    [Header("Spawn / Assemble Grace")]
    public float stateGraceTime = 0.3f;
    private float encounterStartTime = -1f;

    [Header("Chase Delay")]
    public float chaseDelay = 2f;
    private float chaseDelayTimer = 0f;

    [Header("Relative Speeds")]
    [Tooltip("Multiplier compared to player movement speed")]
    public float breakChaseSpeedMultiplier = 0.25f;

    [Header("Transition Smoothing")]
    public float globalFormationLerp = 6f;

    [Header("Formation Spacing")]
    public float lineSpacing = 1.8f;
    public float wedgeSpacing = 1.6f;
    public float boxSpacing = 2.4f;
    public float diamondSpacing = 2.8f;

    public enum PhalanxState
    {
        March,
        HoldFire,
        Encircle,
        BreakChase,
        Collapse
    }

    public enum FormationType
    {
        Line,
        Wedge,
        Box,
        Diamond
    }

    [Header("Active Formation")]
    public FormationType activeFormation = FormationType.Line;

    public PhalanxState phalanxState = PhalanxState.March;

    [Header("Debug / Testing")]
    [Tooltip("If enabled, press 1-4 to force formations (Line/Box/Diamond/Wedge) without the state machine overriding them.")]
    public bool manualFormationOverride = false;

    void Start()
    {
        SetState(PhalanxState.March);
        ResolvePlayer();
    }

    void Update()
    {
        // Optional: manual formation testing
        if (manualFormationOverride)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) activeFormation = FormationType.Line;
            if (Input.GetKeyDown(KeyCode.Alpha2)) activeFormation = FormationType.Box;
            if (Input.GetKeyDown(KeyCode.Alpha3)) activeFormation = FormationType.Diamond;
            if (Input.GetKeyDown(KeyCode.Alpha4)) activeFormation = FormationType.Wedge;
        }

        if (agents.Count == 0)
        {
            ResetEncounter();
            return;
        }

        ResolveLeader();

        if (playerTransform == null)
            ResolvePlayer();

        if (playerTransform == null || formationLeader == null)
            return;

        bool inGraceWindow = (encounterStartTime > 0f) &&
                             (Time.time - encounterStartTime < stateGraceTime);

        // Grace window blocks transitions only (does NOT force March/Line)
        if (inGraceWindow)
        {
            rotationLocked = false;
            return;
        }

        // Robust radii: we treat the larger as "outer hold" and the smaller as "inner encircle"
        float outerHold = Mathf.Max(holdRadius, encircleRadius);
        float innerEncircle = Mathf.Min(holdRadius, encircleRadius);

        float dist = Vector3.Distance(formationLeader.transform.position, playerTransform.position);

        // Timers
        if (stateTimer > 0f)
            stateTimer -= Time.deltaTime;

        // ----- STATE MACHINE -----
        switch (phalanxState)
        {
            case PhalanxState.March:
                {
                    rotationLocked = false;

                    // Inner first (Encircle), then outer (HoldFire)
                    if (dist <= innerEncircle)
                    {
                        EnterEncircle();
                    }
                    else if (dist <= outerHold)
                    {
                        EnterHoldFire();
                    }
                    break;
                }

            case PhalanxState.HoldFire:
                {
                    rotationLocked = true;

                    if (dist <= innerEncircle)
                    {
                        EnterEncircle();
                    }
                    else if (dist > outerHold * 1.10f)
                    {
                        EnterBreakChase();
                    }
                    break;
                }

            case PhalanxState.Encircle:
                {
                    rotationLocked = true;
                    UpdateHammerLogic();

                    if (dist > outerHold * 1.10f)
                    {
                        EnterBreakChase();
                    }
                    else if (stateTimer <= 0f)
                    {
                        EnterHoldFire();
                    }
                    break;
                }

            case PhalanxState.BreakChase:
                {
                    rotationLocked = false;

                    // hesitation phase
                    if (chaseDelayTimer > 0f)
                    {
                        chaseDelayTimer -= Time.deltaTime;
                        break;
                    }

                    if (stateTimer <= 0f)
                    {
                        SetState(PhalanxState.March);
                    }
                    break;
                }

            case PhalanxState.Collapse:
            default:
                rotationLocked = true;
                break;
        }

        // Collapse fallback
        if (CountRole(EnemyRole.Offender) < 2)
        {
            SetState(PhalanxState.Collapse);
        }
    }

    void ResetEncounter()
    {
        encounterStartTime = -1f;
        rotationLocked = false;
        currentHammer = null;
        formationLeader = null;
        stateTimer = 0f;
        chaseDelayTimer = 0f;

        SetState(PhalanxState.March);
    }

    void SetState(PhalanxState newState)
    {
        if (phalanxState == newState)
            return;

        phalanxState = newState;

        // Only auto-set formation if we're NOT manually overriding
        if (!manualFormationOverride)
        {
            switch (phalanxState)
            {
                case PhalanxState.March:
                    activeFormation = FormationType.Line;
                    break;

                case PhalanxState.HoldFire:
                    activeFormation = FormationType.Box;
                    break;

                case PhalanxState.Encircle:
                    activeFormation = FormationType.Diamond;
                    break;

                case PhalanxState.BreakChase:
                    activeFormation = FormationType.Wedge;
                    break;

                case PhalanxState.Collapse:
                    activeFormation = FormationType.Box;
                    break;
            }
        }
    }

    void EnterHoldFire()
    {
        SetState(PhalanxState.HoldFire);
        // no timer needed; exits based on distance / other states
    }

    void EnterEncircle()
    {
        SetState(PhalanxState.Encircle);

        encircleCenter = playerTransform.position; // LOCK position
        stateTimer = encircleDuration;
        hammerTimer = hammerInterval;
    }


    void EnterBreakChase()
    {
        SetState(PhalanxState.BreakChase);
        stateTimer = breakChaseDuration;

        // start hesitation timer
        chaseDelayTimer = chaseDelay;
    }

    void UpdateHammerLogic()
    {
        hammerTimer -= Time.deltaTime;

        if (hammerTimer <= 0f)
        {
            PickNewHammer();
            hammerTimer = hammerInterval;
        }

        if (currentHammer != null && Time.time > hammerUntil)
            currentHammer = null;
    }

    void PickNewHammer()
    {
        List<EnemyAgent> offenders = agents.FindAll(a => a.role == EnemyRole.Offender);
        if (offenders.Count == 0)
            return;

        currentHammer = offenders[Random.Range(0, offenders.Count)];
        hammerUntil = Time.time + hammerWindow;
    }

    public bool IsHammer(EnemyAgent agent) => agent == currentHammer;
    public bool IsLeader(EnemyAgent agent) => agent == formationLeader;

    public Vector3 GetWorldPositionFor(EnemyAgent agent)
    {
        if (playerTransform == null)
            return agent.transform.position;

        if (formationLeader == null)
            return playerTransform.position;

        if (phalanxState == PhalanxState.Encircle)
            return GetEncirclePosition(agent);

        if (phalanxState == PhalanxState.BreakChase)
            return playerTransform.position;

        return GetPhalanxPosition(agent);
    }

    Vector3 GetEncirclePosition(EnemyAgent agent)
    {
        Vector3 center = encircleCenter;


        float radius = agent.role switch
        {
            EnemyRole.Offender => encircleRadiusOffenders,
            EnemyRole.Defender => encircleRadiusDefenders,
            EnemyRole.Ranger => encircleRadiusRangers,
            _ => encircleRadiusDefenders
        };

        int index = agents.IndexOf(agent);
        float angle = (360f / Mathf.Max(1, agents.Count)) * index;
        Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.up;

        return center + dir * radius;
    }

    Vector3 GetPhalanxPosition(EnemyAgent agent)
    {
        switch (activeFormation)
        {
            case FormationType.Line: return GetLineFormation(agent);
            case FormationType.Wedge: return GetWedgeFormation(agent);
            case FormationType.Box: return GetBoxFormation(agent);
            case FormationType.Diamond: return GetDiamondFormation(agent);
            default: return GetLineFormation(agent);
        }
    }

    Vector3 GetLineFormation(EnemyAgent agent)
    {
        Vector3 leaderPos = formationLeader.transform.position;
        float angle = GetLeaderRotation();
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        Vector3 forward = rot * Vector3.up;
        Vector3 right = rot * Vector3.right;

        float spacing = 1.8f;
        int index = agents.IndexOf(agent);
        float side = (index - agents.Count / 2f) * spacing;

        return leaderPos + forward * doctrine.offenderRadius + right * side;
    }

    Vector3 GetWedgeFormation(EnemyAgent agent)
    {
        Vector3 leaderPos = formationLeader.transform.position;
        float angle = GetLeaderRotation();
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        Vector3 forward = rot * Vector3.up;
        Vector3 right = rot * Vector3.right;

        int index = agents.IndexOf(agent);
        int row = index / 2;
        int side = (index % 2 == 0) ? -1 : 1;

        float depth = row * 1.6f;
        float width = row * 1.2f * side;

        return leaderPos + forward * depth + right * width;
    }

    Vector3 GetBoxFormation(EnemyAgent agent)
    {
        Vector3 leaderPos = formationLeader.transform.position;
        float angle = GetLeaderRotation();
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        Vector3 forward = rot * Vector3.up;
        Vector3 right = rot * Vector3.right;

        int index = agents.IndexOf(agent);
        int size = Mathf.CeilToInt(Mathf.Sqrt(agents.Count));

        int row = index / size;
        int col = index % size;

        float spacing = boxSpacing;

        float x = (col - (size - 1) / 2f) * spacing;
        float y = row * spacing;

        return leaderPos + forward * y + right * x;
    }


    Vector3 GetDiamondFormation(EnemyAgent agent)
    {
        Vector3 leaderPos = formationLeader.transform.position;
        float angle = GetLeaderRotation();
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        Vector3 forward = rot * Vector3.up;
        Vector3 right = rot * Vector3.right;

        int index = agents.IndexOf(agent);
        float spacing = diamondSpacing;

        int layer = index / 4 + 1;
        int pos = index % 4;

        Vector3 offset = Vector3.zero;

        switch (pos)
        {
            case 0: offset = forward * spacing * layer; break;
            case 1: offset = right * spacing * layer; break;
            case 2: offset = -forward * spacing * layer; break;
            case 3: offset = -right * spacing * layer; break;
        }

        return leaderPos + offset;
    }


    float GetLeaderRotation()
    {
        if (rotationLocked)
            return lastLeaderAngle;

        Vector3 toPlayer = playerTransform.position - formationLeader.transform.position;
        lastLeaderAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
        return lastLeaderAngle;
    }

    public void Register(EnemyAgent agent)
    {
        if (!agents.Contains(agent))
        {
            agents.Add(agent);

            if (encounterStartTime < 0f)
                encounterStartTime = Time.time;
        }
    }

    public void Unregister(EnemyAgent agent)
    {
        agents.Remove(agent);

        if (agent == formationLeader)
            formationLeader = null;
    }

    void ResolveLeader()
    {
        if (formationLeader != null)
            return;

        formationLeader = agents.Find(a => a.role == EnemyRole.Offender);

        if (formationLeader == null && agents.Count > 0)
            formationLeader = agents[0];
    }

    int CountRole(EnemyRole role)
    {
        int c = 0;
        foreach (var a in agents)
            if (a.role == role) c++;
        return c;
    }

    public List<EnemyAgent> GetEnemies()
    {
        return new List<EnemyAgent>(agents);
    }

    void ResolvePlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
            playerTransform = go.transform;
    }

    void OnDrawGizmos()
    {
        if (formationLeader == null || playerTransform == null)
            return;

        Vector3 leaderPos = formationLeader.transform.position;
        Vector3 playerPos = playerTransform.position;

        float outerHold = Mathf.Max(holdRadius, encircleRadius);
        float innerEncircle = Mathf.Min(holdRadius, encircleRadius);

        // ===== STATE COLOR CODING =====
        Color stateColor = Color.white;

        switch (phalanxState)
        {
            case PhalanxState.March: stateColor = Color.green; break;
            case PhalanxState.HoldFire: stateColor = Color.cyan; break;
            case PhalanxState.Encircle: stateColor = Color.red; break;
            case PhalanxState.BreakChase: stateColor = Color.yellow; break;
            case PhalanxState.Collapse: stateColor = Color.gray; break;
        }

        // Outer hold
        Gizmos.color = new Color(stateColor.r, stateColor.g, stateColor.b, 0.6f);
        Gizmos.DrawWireSphere(leaderPos, outerHold);

        // Inner encircle
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(leaderPos, innerEncircle);

        // Player + leader
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(playerPos, 0.35f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(leaderPos, 0.55f);
        Gizmos.DrawLine(leaderPos, playerPos);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            leaderPos + Vector3.up * 2.6f,
            $"STATE: {phalanxState} | FORM: {activeFormation} | dist={Vector3.Distance(leaderPos, playerPos):F2}"
        );
#endif

        // Encircle sub-rings
        if (phalanxState == PhalanxState.Encircle)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(playerPos, encircleRadiusOffenders);

            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.6f);
            Gizmos.DrawWireSphere(playerPos, encircleRadiusDefenders);

            Gizmos.color = new Color(1f, 1f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(playerPos, encircleRadiusRangers);
        }

        // Agents
        foreach (var agent in agents)
        {
            if (agent == null) continue;

            Vector3 slotPos = GetWorldPositionFor(agent);

            Color roleColor = agent.role switch
            {
                EnemyRole.Offender => Color.red,
                EnemyRole.Defender => Color.blue,
                EnemyRole.Ranger => Color.yellow,
                _ => Color.white
            };

            if (agent == currentHammer)
                roleColor = Color.magenta;

            Gizmos.color = roleColor;
            Gizmos.DrawSphere(slotPos, 0.25f);
            Gizmos.DrawLine(agent.transform.position, slotPos);
        }
    }
}
