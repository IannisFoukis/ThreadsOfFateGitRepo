using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EncounterCoordinator : MonoBehaviour
{
    public Transform playerTransform;

    [SerializeField] private FormationResolver formationResolver;
    [SerializeField] private TacticalAuthority tacticalAuthority;

    // ─────────────────────────────
    // GROUPS (Phase G1)
    // ─────────────────────────────
    [Header("Groups (Phase G1)")]
    [SerializeField] private int maxGroupSize = 6;

    [Header("Group Anchor Stability")]
    [SerializeField] private bool smoothGroupAnchors = true;
    [SerializeField] private float groupAnchorLerp = 12f;

    [Header("Group Layout")]
    [Tooltip("Distance between group anchors left/right during March.")]
    [SerializeField] private float groupLateralSpacing = 3.2f;

    [Tooltip("Extra forward push applied to group anchors during March.")]
    [SerializeField] private float groupAnchorForwardOffset = 2.8f;

    // ─────────────────────────────
    // ROOM CONFIG / LEGACY COMPATIBILITY
    // (Required by RoomConfigController)
    // ─────────────────────────────
    [Header("Room Config (Legacy / Phase M)")]
    public float holdCompression = 0.7f;
    public float encircleCompression = 0.45f;

    // ─────────────────────────────
    // DEBUG / VISUALIZATION
    // ─────────────────────────────
    [Header("Debug")]
    public bool drawGizmos = true;

    public enum GroupIntent
    {
        None,
        Push,
        Hold,
        Flank,
        Screen
    }

    private class FormationGroup
    {
        public int id;
        public readonly List<EnemyAgent> members = new();

        public Vector3 anchor;      // smoothed anchor
        public Vector3 rawAnchor;   // debug / target
        public bool anchorInitialized;

        public GroupIntent intent = GroupIntent.None;
    }

    private readonly List<FormationGroup> groups = new();

    // ─────────────────────────────
    // PHALANX STATE
    // ─────────────────────────────
    public enum PhalanxState
    {
        Assemble,
        March,
        HoldFire,
        Encircle,
        BreakChase,
        Collapse
    }

    public PhalanxState phalanxState = PhalanxState.Assemble;
    private PhalanxState lastState;

    // ─────────────────────────────
    // ASSEMBLY
    // ─────────────────────────────
    [Header("Assemble Gate")]
    public bool requireAssembleBeforeMarch = true;
    public float assembleCompressionThreshold = 0.45f;
    public float assembleMinStableTime = 0.25f;

    private bool assembled;
    private float assembledStableTimer;

    // ─────────────────────────────
    // MARCH
    // ─────────────────────────────
    [Header("March Anchor")]
    public float marchAnchorRecalcInterval = 0.18f;
    public float marchAnchorLerp = 12f;

    [Tooltip("How far behind the player the march anchor stays (core chase distance).")]
    [SerializeField] private float marchAnchorDistanceFromPlayer = 2f;

    private Vector3 marchAnchor;
    private Vector3 marchToPlayerDir;
    private float nextMarchAnchorEvalTime;

    [Header("Assemble Anchor Forward Offset")]
    [SerializeField] private float rallyAnchorForwardOffset = 2.5f;

    // ─────────────────────────────
    // FREEZE (kept, but not used to stall chase)
    // ─────────────────────────────
    [Header("Freeze")]
    public float freezeDistanceToPlayer = 1.6f;
    public float unfreezeDistanceToPlayer = 2.3f;

    private bool formationFrozen;
    private Vector3 frozenAvg;
    private Vector3 frozenToPlayerDir;

    // ─────────────────────────────
    // ENCIRCLE
    // ─────────────────────────────
    [Header("Encircle")]
    public float encircleRadius = 4f;
    public float encircleDuration = 3.5f;

    private float stateTimer;
    private Vector3 encircleCenter;

    // ─────────────────────────────
    // COMPRESSION (Group Aware)
    // ─────────────────────────────
    public enum GroupCompressionMode { Average, Max }

    [Header("Compression")]
    [SerializeField] private bool useGroupCompression = true;
    [SerializeField] private GroupCompressionMode groupCompressionMode = GroupCompressionMode.Average;

    // ─────────────────────────────
    // AGENTS
    // ─────────────────────────────
    private readonly List<EnemyAgent> agents = new();
    private readonly List<EnemyAgent> ordered = new();

    private EnemyAgent formationLeader;
    private EnemyAgent currentHammer;
    private bool leaderDead;

    // ─────────────────────────────
    // RALLY
    // ─────────────────────────────
    private bool rallyInitialized;
    private Vector3 rallyAvg;
    private Vector3 rallyToPlayerDir;

    // ─────────────────────────────
    // LEGACY
    // ─────────────────────────────
    private DoctrineState doctrine;

    // ─────────────────────────────
    // UNITY
    // ─────────────────────────────
    void Awake()
    {
        if (formationResolver == null)
            formationResolver = GetComponent<FormationResolver>();

        if (tacticalAuthority == null)
            tacticalAuthority = GetComponent<TacticalAuthority>();

        if (formationResolver != null)
            formationResolver.OnFormationChanged += HandleFormationChanged;
    }

    void Start()
    {
        ResolvePlayer();
        SetState(PhalanxState.Assemble);
    }

    void Update()
    {
        if (agents.Count == 0 || playerTransform == null)
            return;

        ResolveLeader();
        EnsureRallyInitialized();
        UpdateFreezeLogic();

        if (stateTimer > 0f)
            stateTimer -= Time.deltaTime;

        // Important: march anchor should update even if groups aren't perfect yet,
        // but we still keep assemble gating for state transition discipline.
        if (phalanxState == PhalanxState.March)
            UpdateMarchAnchor();

        // Group anchors are intent-driven off marchAnchor (THIS is the chase).
        UpdateGroupsAnchors();

        HandleAssembleGate();

        switch (phalanxState)
        {
            case PhalanxState.Assemble:
                if (IsFormationAssembled())
                {
                    assembled = true;
                    SetState(PhalanxState.March);
                }
                break;

            case PhalanxState.March:
                // Later: hold/encircle transitions.
                break;

            case PhalanxState.Encircle:
                if (stateTimer <= 0f)
                    SetState(PhalanxState.March);
                break;
        }
    }

    void OnDestroy()
    {
        if (formationResolver != null)
            formationResolver.OnFormationChanged -= HandleFormationChanged;
    }

    // ─────────────────────────────
    // REGISTRATION
    // ─────────────────────────────
    public void Register(EnemyAgent agent)
    {
        if (agent == null) return;

        if (!agents.Contains(agent))
            agents.Add(agent);

        AssignToGroup(agent);
        AssignDefaultIntents();

        rallyInitialized = false;
    }

    public void Unregister(EnemyAgent agent)
    {
        agents.Remove(agent);
        RemoveFromGroup(agent);

        if (agent == formationLeader)
            leaderDead = true;

        rallyInitialized = false;
    }

    // ─────────────────────────────
    // GROUP LOGIC
    // ─────────────────────────────
    void AssignToGroup(EnemyAgent agent)
    {
        FormationGroup g = null;

        if (groups.Count > 0 && groups[^1].members.Count < maxGroupSize)
            g = groups[^1];

        if (g == null)
        {
            g = new FormationGroup { id = groups.Count };
            groups.Add(g);
        }

        g.members.Add(agent);
        g.anchorInitialized = false;
    }

    void RemoveFromGroup(EnemyAgent agent)
    {
        foreach (var g in groups)
        {
            if (g.members.Remove(agent))
                break;
        }
    }

    void AssignDefaultIntents()
    {
        for (int i = 0; i < groups.Count; i++)
            groups[i].intent = (i == 0) ? GroupIntent.Push : GroupIntent.Hold;
    }

    void UpdateGroupsAnchors()
    {
        if (playerTransform == null)
            return;

        // Use the march direction for group layout as well.
        Vector3 forward = marchToPlayerDir;
        if (forward.sqrMagnitude < 0.0001f)
            forward = (playerTransform.position - marchAnchor).normalized;

        Vector3 right = new Vector3(-forward.y, forward.x, 0f);

        // Center groups around 0 (e.g., 2 groups => -0.5,+0.5 ; 3 groups => -1,0,+1)
        float center = (groups.Count - 1) * 0.5f;

        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];

            // Intent-driven anchor target:
            // base on marchAnchor so the whole formation advances.
            float lateralIndex = (i - center);

            Vector3 target =
                marchAnchor
                + right * (lateralIndex * groupLateralSpacing)
                + forward * groupAnchorForwardOffset;

            g.rawAnchor = target;

            if (!smoothGroupAnchors)
            {
                g.anchor = g.rawAnchor;
                g.anchorInitialized = true;
                continue;
            }

            if (!g.anchorInitialized)
            {
                g.anchor = g.rawAnchor;
                g.anchorInitialized = true;
                continue;
            }

            float t = 1f - Mathf.Exp(-groupAnchorLerp * Time.deltaTime);
            g.anchor = Vector3.Lerp(g.anchor, g.rawAnchor, t);
        }
    }

    FormationGroup GetGroupOf(EnemyAgent agent)
    {
        foreach (var g in groups)
            if (g.members.Contains(agent))
                return g;
        return null;
    }

    // ─────────────────────────────
    // FORMATION POSITIONS
    // ─────────────────────────────
    public Vector3 GetWorldPositionFor(EnemyAgent agent)
    {
        if (agent == null) return transform.position;
        if (agent.attackPositionLocked || agent.attackLock)
            return agent.transform.position;

        if (phalanxState == PhalanxState.Assemble)
        {
            Vector3 assembleAnchor = rallyAvg + rallyToPlayerDir * rallyAnchorForwardOffset;
            return GetSlot(agent, assembleAnchor, rallyToPlayerDir);
        }

        if (phalanxState == PhalanxState.March)
        {
            var g = GetGroupOf(agent);
            Vector3 anchor = (g != null) ? g.anchor : marchAnchor;
            return GetSlot(agent, anchor, marchToPlayerDir);
        }

        return GetSlot(agent, marchAnchor, marchToPlayerDir);
    }

    Vector3 GetSlot(EnemyAgent agent, Vector3 anchor, Vector3 toPlayerDir)
    {
        EnsureOrdered();

        if (toPlayerDir.sqrMagnitude < 0.0001f && playerTransform != null)
            toPlayerDir = (playerTransform.position - anchor).normalized;

        Vector3 right = new Vector3(-toPlayerDir.y, toPlayerDir.x, 0f);

        float depth = agent.role switch
        {
            EnemyRole.Offender => 0.5f,
            EnemyRole.Defender => 3.2f,
            EnemyRole.Ranger => 5.2f,
            _ => 2.8f
        };

        int index = ordered.IndexOf(agent);
        if (index < 0) index = 0;

        float lateral = ((index % 3) - 1) * 0.9f;

        // Slots sit BEHIND the anchor relative to player
        return anchor - toPlayerDir * depth + right * lateral;
    }

    // ─────────────────────────────
    // HELPERS
    // ─────────────────────────────
    void ResolvePlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) playerTransform = go.transform;
    }

    void ResolveLeader()
    {
        if (formationLeader != null || leaderDead) return;
        formationLeader = agents.Find(a => a != null && a.role == EnemyRole.Offender);
    }

    void EnsureRallyInitialized()
    {
        if (rallyInitialized) return;

        rallyAvg = GetAverageEnemyPosition();
        Vector3 d = playerTransform.position - rallyAvg;
        rallyToPlayerDir = d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.up;

        // Initialize march anchor near the group first; it will “snap to chase” quickly.
        marchToPlayerDir = rallyToPlayerDir;
        marchAnchor = rallyAvg;

        assembled = false;
        assembledStableTimer = 0f;
        rallyInitialized = true;
    }

    void UpdateFreezeLogic()
    {
        // Keeping your freeze system intact; not used to stop anchor chasing.
        float min = float.MaxValue;
        float max = 0f;

        foreach (var a in agents)
        {
            if (a == null) continue;
            float d = Vector2.Distance(a.transform.position, playerTransform.position);
            min = Mathf.Min(min, d);
            max = Mathf.Max(max, d);
        }

        if (!formationFrozen && min < freezeDistanceToPlayer)
        {
            formationFrozen = true;
            frozenAvg = GetAverageEnemyPosition();
            frozenToPlayerDir = (playerTransform.position - frozenAvg).normalized;
        }

        if (formationFrozen && max > unfreezeDistanceToPlayer)
            formationFrozen = false;
    }

    void HandleAssembleGate()
    {
        // Only relevant when we are enforcing assemble-before-march discipline.
        if (!requireAssembleBeforeMarch)
            return;

        // We track stability in Assemble phase (not March).
        if (phalanxState != PhalanxState.Assemble)
            return;

        float comp = GetFormationCompression();

        if (comp <= assembleCompressionThreshold)
        {
            assembledStableTimer += Time.deltaTime;
            if (assembledStableTimer >= assembleMinStableTime)
                assembled = true;
        }
        else
        {
            assembledStableTimer = 0f;
            assembled = false;
        }
    }

    bool IsFormationAssembled() =>
        assembled || !requireAssembleBeforeMarch;

    float GetFormationCompression()
    {
        if (!useGroupCompression)
        {
            float sum = 0f;
            foreach (var a in agents)
            {
                if (a == null) continue;
                sum += Vector3.Distance(a.transform.position, GetWorldPositionFor(a));
            }
            return sum / Mathf.Max(1, agents.Count);
        }

        float agg = 0f;
        float max = 0f;
        int count = 0;

        foreach (var g in groups)
        {
            float sum = 0f;
            int c = 0;

            foreach (var a in g.members)
            {
                if (a == null) continue;
                sum += Vector3.Distance(a.transform.position, GetWorldPositionFor(a));
                c++;
            }

            if (c == 0) continue;

            float gc = sum / c;
            agg += gc;
            max = Mathf.Max(max, gc);
            count++;
        }

        return count == 0
            ? 0f
            : (groupCompressionMode == GroupCompressionMode.Max ? max : agg / count);
    }

    Vector3 GetAverageEnemyPosition()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (var a in agents)
        {
            if (a == null) continue;
            if (a.role == EnemyRole.Ranger) continue;
            sum += a.transform.position;
            count++;
        }

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

    void EnsureOrdered()
    {
        ordered.Clear();

        // Keep your role layering (front/mid/back) stable
        foreach (var a in agents) if (a != null && a.role == EnemyRole.Offender) ordered.Add(a);
        foreach (var a in agents) if (a != null && a.role == EnemyRole.Defender) ordered.Add(a);
        foreach (var a in agents) if (a != null && a.role == EnemyRole.Ranger) ordered.Add(a);
    }

    public bool AnyRoleChangingFormation(EnemyRole role, float threshold = -1f)
    {
        foreach (var a in agents)
            if (a != null && a.role == role && a.IsChangingFormation(threshold))
                return true;
        return false;
    }

    void UpdateMarchAnchor()
    {
        if (Time.time < nextMarchAnchorEvalTime)
            return;

        nextMarchAnchorEvalTime = Time.time + marchAnchorRecalcInterval;

        Vector3 avg = GetAverageEnemyPosition();
        Vector3 toPlayer = (playerTransform.position - avg);

        if (toPlayer.sqrMagnitude < 0.0001f)
            return;

        marchToPlayerDir = toPlayer.normalized;

        // ✅ Strong chase: keep a stable distance behind the player
        Vector3 desiredAnchor = playerTransform.position - marchToPlayerDir * marchAnchorDistanceFromPlayer;

        float t = 1f - Mathf.Exp(-marchAnchorLerp * Time.deltaTime);
        marchAnchor = Vector3.Lerp(marchAnchor, desiredAnchor, t);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos || !Application.isPlaying) return;

        // March anchor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(marchAnchor, 0.18f);

        // Groups
        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            if (g == null) continue;

            Color c = Color.HSVToRGB((i * 0.18f) % 1f, 0.9f, 0.9f);
            Gizmos.color = c;

            Gizmos.DrawWireSphere(g.anchor, 0.25f);
            Gizmos.DrawLine(g.anchor, g.rawAnchor);

            Handles.Label(g.anchor + Vector3.up * 0.3f, $"G{i} {g.intent}");
        }
    }
#endif

    // ─────────────────────────────
    // LEGACY SURFACE
    // ─────────────────────────────
    public List<EnemyAgent> GetEnemies() => new List<EnemyAgent>(agents);
    public bool IsHammer(EnemyAgent agent) => agent == currentHammer;
    public bool SilenceActive => false;
    public void ApplyDoctrine(DoctrineState state) => doctrine = state;

    void SetState(PhalanxState next)
    {
        if (phalanxState == next) return;

        lastState = phalanxState;
        phalanxState = next;

        formationFrozen = false;

        if (phalanxState == PhalanxState.Assemble)
        {
            assembled = false;
            assembledStableTimer = 0f;
            rallyInitialized = false;
        }
    }

    void HandleFormationChanged(FormationType newFormation)
    {
        Debug.Log($"[EncounterCoordinator] Formation change → {newFormation}");
        rallyInitialized = false;
        assembled = false;
        assembledStableTimer = 0f;
        SetState(PhalanxState.Assemble);
    }
}
