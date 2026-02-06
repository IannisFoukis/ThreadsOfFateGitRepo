using System.Collections.Generic;
using UnityEngine;
using TOF.EnemyAI.Groups;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EncounterCoordinator : MonoBehaviour
{
    public Transform playerTransform;

    // ─────────────────────────────
    // G1 GROUPS (EnemyGroup is source of truth)
    // ─────────────────────────────
    [Header("Groups (Phase G1)")]
    [SerializeField] private int defaultGroups = 2;
    [SerializeField] private int maxGroupSize = 6; // legacy knob (ok unused in G1)

    private readonly List<EnemyGroup> _groups = new List<EnemyGroup>();
    private int _nextGroupId = 1;

    public IReadOnlyList<EnemyGroup> Groups => _groups;

    [Header("Group Anchor Stability")]
    [SerializeField] private bool smoothGroupAnchors = true;
    [SerializeField] private float groupAnchorLerp = 12f;

    [Header("Group Layout")]
    [Tooltip("Distance between group anchors left/right during March.")]
    [SerializeField] private float groupLateralSpacing = 3.2f;

    [Tooltip("Extra forward push applied to group anchors during March.")]
    [SerializeField] private float groupAnchorForwardOffset = 2.8f;

    private class GroupAnchorState
    {
        public Vector3 anchor;
        public Vector3 rawAnchor;
        public bool initialized;
    }

    private readonly Dictionary<int, GroupAnchorState> _anchorByGroupId = new Dictionary<int, GroupAnchorState>();

    private class SquadRallyState
    {
        public Vector3 rallyAvg;
        public Vector3 rallyToPlayerDir;
        public bool initialized;
    }

    private readonly Dictionary<int, SquadRallyState> _rallyByGroupId = new Dictionary<int, SquadRallyState>();

    // ─────────────────────────────
    // DEPENDENCIES
    // ─────────────────────────────
    [SerializeField] private FormationResolver formationResolver;
    [SerializeField] private TacticalAuthority tacticalAuthority;

    [Header("Room Config (Legacy / Phase M)")]
    public float holdCompression = 0.7f;
    public float encircleCompression = 0.45f;

    [Header("Debug")]
    public bool drawGizmos = true;

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
    // FREEZE
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
    // COMPRESSION
    // ─────────────────────────────
    public enum GroupCompressionMode { Average, Max }

    [Header("Compression")]
    [SerializeField] private bool useGroupCompression = true;
    [SerializeField] private GroupCompressionMode groupCompressionMode = GroupCompressionMode.Average;

    // ─────────────────────────────
    // AGENTS
    // ─────────────────────────────
    private readonly List<EnemyAgent> agents = new List<EnemyAgent>();
    private readonly List<EnemyAgent> ordered = new List<EnemyAgent>();

    private EnemyAgent formationLeader;
    private EnemyAgent currentHammer;
    private bool leaderDead;

    // ─────────────────────────────
    // RALLY (global fallback)
    // ─────────────────────────────
    private bool rallyInitialized;
    private Vector3 rallyAvg;
    private Vector3 rallyToPlayerDir;

    // ─────────────────────────────
    // LEGACY
    // ─────────────────────────────
    private DoctrineState doctrine;
    private FormationType currentFormation = FormationType.Swarm;

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
        if (playerTransform == null)
        {
            ResolvePlayer();
            if (playerTransform == null) return;
        }

        if (GetActiveAgentsCount() == 0)
            return;

        ResolveLeader();
        EnsureRallyInitialized();
        UpdateFreezeLogic();

        if (stateTimer > 0f)
            stateTimer -= Time.deltaTime;

        if (phalanxState == PhalanxState.March)
            UpdateMarchAnchor();

        UpdateGroupAnchors();
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

        rallyInitialized = false;
    }

    public void Unregister(EnemyAgent agent)
    {
        agents.Remove(agent);

        if (agent != null && agent.Group != null)
        {
            agent.Group.RemoveMember(agent);
        }

        if (agent == formationLeader)
            leaderDead = true;

        rallyInitialized = false;
    }

    int GetActiveAgentsCount()
    {
        int c = 0;
        for (int i = 0; i < agents.Count; i++)
            if (agents[i] != null && agents[i].CombatEngaged) c++;
        return c;
    }

    // ─────────────────────────────
    // GROUP CREATION
    // ─────────────────────────────
    private EnemyGroup CreateGroup(FormationType formation, DoctrineState doctrineState)
    {
        var g = new EnemyGroup(_nextGroupId++, formation, doctrineState);
        _groups.Add(g);
        _anchorByGroupId[g.GroupId] = new GroupAnchorState { initialized = false };
        _rallyByGroupId[g.GroupId] = new SquadRallyState { initialized = false };
        return g;
    }

    // ─────────────────────────────
    // ROOM-LEVEL BUILD (clears & rebuilds)
    // ─────────────────────────────
    public void BuildGroupsFromEnemies(List<EnemyAgent> spawned, FormationType roomFormation, DoctrineState doctrineState)
    {
        if (playerTransform == null) ResolvePlayer();

        _groups.Clear();
        _anchorByGroupId.Clear();
        _rallyByGroupId.Clear();
        _nextGroupId = 1;

        currentFormation = roomFormation;
        doctrine = doctrineState;

        int count = Mathf.Max(1, defaultGroups);
        for (int i = 0; i < count; i++)
        {
            var form = (i == 0) ? roomFormation : FormationType.Swarm;
            CreateGroup(form, doctrineState);
        }

        int gi = 0;
        for (int i = 0; i < spawned.Count; i++)
        {
            var a = spawned[i];
            if (a == null) continue;

            _groups[gi].AddMember(a);
            gi = (gi + 1) % _groups.Count;
        }

        // Seed rally per group so Assemble doesn’t clump
        SeedRallyForAllGroups();

        // ✅ PHASE G1: authoritative activation happens HERE
        ActivateAgents(spawned);

        rallyInitialized = false;
        SetState(PhalanxState.Assemble);
    }

    // ─────────────────────────────
    // SQUAD INJECTION (adds groups; does NOT clear existing)
    // ─────────────────────────────
    public void SpawnSquadGroupsFromEnemies(
        List<EnemyAgent> squadAgents,
        int groupsForSquad,
        FormationType squadFormation,
        DoctrineState doctrineState,
        Vector3 squadCenterHint
    )
    {
        if (squadAgents == null || squadAgents.Count == 0)
            return;

        if (playerTransform == null) ResolvePlayer();

        currentFormation = squadFormation;
        doctrine = doctrineState;

        int startIndex = _groups.Count;
        int gcount = Mathf.Max(1, groupsForSquad);

        for (int i = 0; i < gcount; i++)
        {
            var form = (i == 0) ? squadFormation : FormationType.Swarm;
            CreateGroup(form, doctrineState);
        }

        int gi = 0;
        for (int i = 0; i < squadAgents.Count; i++)
        {
            var a = squadAgents[i];
            if (a == null) continue;

            var g = _groups[startIndex + gi];
            g.AddMember(a);

            gi = (gi + 1) % gcount;
        }

        SeedRallyForGroups(startIndex, gcount, squadCenterHint);

        // ✅ PHASE G1: authoritative activation happens HERE too
        ActivateAgents(squadAgents);

        rallyInitialized = false;
        SetState(PhalanxState.Assemble);
    }

    void ActivateAgents(List<EnemyAgent> list)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null) continue;

            a.ActivateForCombat();
        }
    }

    // ─────────────────────────────
    // GROUP ANCHORS
    // ─────────────────────────────
    void UpdateGroupAnchors()
    {
        if (playerTransform == null) return;
        if (_groups.Count == 0) return;

        Vector3 forward = marchToPlayerDir;
        if (forward.sqrMagnitude < 0.0001f)
            forward = (playerTransform.position - marchAnchor).normalized;

        Vector3 right = new Vector3(-forward.y, forward.x, 0f);
        float center = (_groups.Count - 1) * 0.5f;

        for (int i = 0; i < _groups.Count; i++)
        {
            var g = _groups[i];
            if (g == null) continue;

            if (!_anchorByGroupId.TryGetValue(g.GroupId, out var st))
            {
                st = new GroupAnchorState();
                _anchorByGroupId[g.GroupId] = st;
            }

            float lateralIndex = (i - center);

            Vector3 target =
                marchAnchor
                + right * (lateralIndex * groupLateralSpacing)
                + forward * groupAnchorForwardOffset;

            st.rawAnchor = target;

            if (!smoothGroupAnchors)
            {
                st.anchor = st.rawAnchor;
                st.initialized = true;
                continue;
            }

            if (!st.initialized)
            {
                st.anchor = st.rawAnchor;
                st.initialized = true;
                continue;
            }

            float t = 1f - Mathf.Exp(-groupAnchorLerp * Time.deltaTime);
            st.anchor = Vector3.Lerp(st.anchor, st.rawAnchor, t);
        }
    }

    private Vector3 GetAnchorForAgent(EnemyAgent agent)
    {
        if (agent == null) return marchAnchor;

        var g = agent.Group;
        if (g != null && _anchorByGroupId.TryGetValue(g.GroupId, out var st) && st.initialized)
            return st.anchor;

        return marchAnchor;
    }

    // ─────────────────────────────
    // FORMATION POSITIONS
    // ─────────────────────────────
    public Vector3 GetWorldPositionFor(EnemyAgent agent)
    {
        if (agent == null) return transform.position;

        // Phase G1: dormant agents do not get moved
        if (!agent.CombatEngaged)
            return agent.transform.position;

        if (agent.attackPositionLocked || agent.attackLock)
            return agent.transform.position;

        if (phalanxState == PhalanxState.Assemble)
        {
            if (agent.Group != null && _rallyByGroupId.TryGetValue(agent.Group.GroupId, out var rs) && rs.initialized)
            {
                Vector3 assembleAnchor = rs.rallyAvg + rs.rallyToPlayerDir * rallyAnchorForwardOffset;
                return GetSlot(agent, assembleAnchor, rs.rallyToPlayerDir);
            }

            Vector3 fallback = rallyAvg + rallyToPlayerDir * rallyAnchorForwardOffset;
            return GetSlot(agent, fallback, rallyToPlayerDir);
        }

        if (phalanxState == PhalanxState.March)
        {
            Vector3 anchor = GetAnchorForAgent(agent);
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

        // Phase G1: leader must be engaged
        formationLeader = agents.Find(a => a != null && a.CombatEngaged && a.role == EnemyRole.Offender);
    }

    void EnsureRallyInitialized()
    {
        if (rallyInitialized) return;

        rallyAvg = GetAverageEnemyPosition();
        Vector3 d = playerTransform.position - rallyAvg;
        rallyToPlayerDir = d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.up;

        marchToPlayerDir = rallyToPlayerDir;
        marchAnchor = rallyAvg;

        assembled = false;
        assembledStableTimer = 0f;
        rallyInitialized = true;

        foreach (var kv in _anchorByGroupId)
            kv.Value.initialized = false;
    }

    void SeedRallyForAllGroups()
    {
        for (int i = 0; i < _groups.Count; i++)
        {
            var g = _groups[i];
            if (g == null) continue;

            Vector3 avg = AverageOfGroup(g);
            SeedRallyForGroupId(g.GroupId, avg);
        }
    }

    void SeedRallyForGroups(int startIndex, int count, Vector3 centerHint)
    {
        for (int i = 0; i < count; i++)
        {
            var g = _groups[startIndex + i];
            if (g == null) continue;

            Vector3 avg = AverageOfGroup(g);
            if ((avg - transform.position).sqrMagnitude < 0.0001f)
                avg = centerHint;

            SeedRallyForGroupId(g.GroupId, avg);
        }
    }

    void SeedRallyForGroupId(int groupId, Vector3 rallyCenter)
    {
        if (!_rallyByGroupId.TryGetValue(groupId, out var rs))
        {
            rs = new SquadRallyState();
            _rallyByGroupId[groupId] = rs;
        }

        rs.rallyAvg = rallyCenter;

        Vector3 d = playerTransform != null ? (playerTransform.position - rallyCenter) : Vector3.up;
        rs.rallyToPlayerDir = d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.up;

        rs.initialized = true;
    }

    Vector3 AverageOfGroup(EnemyGroup g)
    {
        if (g == null) return transform.position;

        Vector3 sum = Vector3.zero;
        int c = 0;

        var members = g.Members;
        for (int i = 0; i < members.Count; i++)
        {
            var a = members[i];
            if (a == null) continue;

            // only engaged members matter for rally
            if (!a.CombatEngaged) continue;

            sum += a.transform.position;
            c++;
        }

        return c > 0 ? sum / c : transform.position;
    }

    void UpdateFreezeLogic()
    {
        float min = float.MaxValue;
        float max = 0f;
        bool any = false;

        foreach (var a in agents)
        {
            if (a == null || !a.CombatEngaged) continue;

            any = true;
            float d = Vector2.Distance(a.transform.position, playerTransform.position);
            min = Mathf.Min(min, d);
            max = Mathf.Max(max, d);
        }

        if (!any) return;

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
        if (!requireAssembleBeforeMarch) return;
        if (phalanxState != PhalanxState.Assemble) return;

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

    bool IsFormationAssembled() => assembled || !requireAssembleBeforeMarch;

    float GetFormationCompression()
    {
        if (!useGroupCompression)
        {
            float sum = 0f;
            int c = 0;

            foreach (var a in agents)
            {
                if (a == null || !a.CombatEngaged) continue;
                sum += Vector3.Distance(a.transform.position, GetWorldPositionFor(a));
                c++;
            }

            return c == 0 ? 0f : sum / c;
        }

        float agg = 0f;
        float max = 0f;
        int count = 0;

        foreach (var g in _groups)
        {
            if (g == null) continue;

            float sum = 0f;
            int c = 0;

            var members = g.Members;
            for (int i = 0; i < members.Count; i++)
            {
                var a = members[i];
                if (a == null || !a.CombatEngaged) continue;
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
            if (a == null || !a.CombatEngaged) continue;
            if (a.role == EnemyRole.Ranger) continue;

            sum += a.transform.position;
            count++;
        }

        if (count == 0)
        {
            foreach (var a in agents)
            {
                if (a == null || !a.CombatEngaged) continue;
                sum += a.transform.position;
                count++;
            }
        }

        return count > 0 ? sum / count : transform.position;
    }

    void EnsureOrdered()
    {
        ordered.Clear();

        foreach (var a in agents) if (a != null && a.CombatEngaged && a.role == EnemyRole.Offender) ordered.Add(a);
        foreach (var a in agents) if (a != null && a.CombatEngaged && a.role == EnemyRole.Defender) ordered.Add(a);
        foreach (var a in agents) if (a != null && a.CombatEngaged && a.role == EnemyRole.Ranger) ordered.Add(a);
    }

    void UpdateMarchAnchor()
    {
        if (Time.time < nextMarchAnchorEvalTime) return;

        nextMarchAnchorEvalTime = Time.time + marchAnchorRecalcInterval;

        Vector3 avg = GetAverageEnemyPosition();
        Vector3 toPlayer = (playerTransform.position - avg);

        if (toPlayer.sqrMagnitude < 0.0001f)
            return;

        marchToPlayerDir = toPlayer.normalized;

        Vector3 desiredAnchor = playerTransform.position - marchToPlayerDir * marchAnchorDistanceFromPlayer;

        float t = 1f - Mathf.Exp(-marchAnchorLerp * Time.deltaTime);
        marchAnchor = Vector3.Lerp(marchAnchor, desiredAnchor, t);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos || !Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(marchAnchor, 0.18f);

        int idx = 0;
        foreach (var g in _groups)
        {
            if (g == null) continue;
            if (!_anchorByGroupId.TryGetValue(g.GroupId, out var st)) continue;

            Color c = Color.HSVToRGB((idx * 0.18f) % 1f, 0.9f, 0.9f);
            Gizmos.color = c;

            Gizmos.DrawWireSphere(st.anchor, 0.25f);
            Gizmos.DrawLine(st.anchor, st.rawAnchor);

            Handles.Label(st.anchor + Vector3.up * 0.3f, $"G{g.GroupId} {g.Intent}");
            idx++;
        }
    }
#endif

    // ─────────────────────────────
    // LEGACY / QUERY HELPERS
    // ─────────────────────────────
    public bool AnyRoleChangingFormation(EnemyRole role, float threshold = -1f)
    {
        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];
            if (a == null || !a.CombatEngaged) continue;
            if (a.role != role) continue;

            if (a.IsChangingFormation(threshold))
                return true;
        }

        return false;
    }

    public List<EnemyAgent> GetEnemies() => new List<EnemyAgent>(agents);
    public bool IsHammer(EnemyAgent agent) => agent == currentHammer;
    public bool SilenceActive => false;

    public void ApplyDoctrine(DoctrineState state)
    {
        doctrine = state;

        for (int i = 0; i < _groups.Count; i++)
            _groups[i].SetDoctrine(state);
    }

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

        currentFormation = newFormation;

        for (int i = 0; i < _groups.Count; i++)
        {
            var g = _groups[i];
            if (g == null) continue;

            bool phalanxActive = (newFormation == FormationType.Phalanx);
            g.SetFormation(newFormation, phalanxActive);
        }

        rallyInitialized = false;
        assembled = false;
        assembledStableTimer = 0f;
        SetState(PhalanxState.Assemble);
    }
}
