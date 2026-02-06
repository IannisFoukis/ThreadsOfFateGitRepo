using System;
using System.Collections.Generic;
using TOF.Rooms.Contracts;
using UnityEngine;

public class CombatRoom : RoomController
{
    [Header("Prefab + Spawn Points")]
    public GameObject enemyPrefab;
    public Transform[] enemySpawnPoints;

    [Header("Debug / Safety")]
    public bool applyFallbackIfZeroCounts = true;

    [Header("Lane / Pressure")]
    [SerializeField] private LaneDirector laneDirector;

    private CombatRoomContext combatCtx;
    private float combatTimer;
    private int aliveEnemies;
    private RoomContract contract;
    private int spawnIndex;
    private bool roomCompletionTriggered;

    // 🔒 Phase G1: explicit spawn tracking
    private readonly List<EnemyAgent> spawnedAgents = new();

    // ⭐ NEW: cache group count at spawn time
    private int spawnGroupCount = 2;

    protected override void Start()
    {
        base.Start();

        contract = RoomAccess.Current;
        if (contract == null)
        {
            Debug.LogError("[CombatRoom] RoomAccess.Current is NULL.");
            return;
        }

        if (!contract.enableCombat)
            return;

        if (applyFallbackIfZeroCounts)
        {
            Debug.Log("[CombatRoom] Applying DEBUG spawn override");

            contract.offenders = 2;
            contract.defenders = 2;
            contract.rangers = 4;
            contract.activators = 0;
            contract.jokers = 0;
        }

        SpawnFromContract();

        // ─── Combat context init ───
        combatTimer = 0f;

        combatCtx = new CombatRoomContext
        {
            roomContext = this.roomContext,
            timeInRoom = 0f,
            enemiesAliveRatio = 1f,
            formationIntegrity = 1f,
            playerPressure = 0f,

            shrinePresent = false,
            shrineActive = false,
            playerLowHealth = false,

            difficultyTier = 0
        };

        if (laneDirector != null)
            laneDirector.SetContext(combatCtx);

        Debug.Log($"[CombatRoom] Combat started | Alive={aliveEnemies}");
    }

    // ⭐ NEW: per-group spatial seed
    Vector3 GetGroupSpawnAnchor(int groupIndex, int groupCount)
    {
        float spacing = 3.5f;
        float center = (groupCount - 1) * 0.5f;

        Vector3 basePos = transform.position;
        Vector3 right = Vector3.right; // top-down

        return basePos + right * ((groupIndex - center) * spacing);
    }

    void Update()
    {
        if (!contract.enableCombat)
            return;

        combatTimer += Time.deltaTime;
        combatCtx.timeInRoom = combatTimer;

        combatCtx.enemiesAliveRatio =
            aliveEnemies > 0
                ? aliveEnemies / (float)(
                    contract.offenders +
                    contract.defenders +
                    contract.rangers +
                    contract.activators +
                    contract.jokers)
                : 0f;

        combatCtx.playerPressure = 0.5f;
        combatCtx.formationIntegrity = 1f;

        if (laneDirector != null)
            laneDirector.SetContext(combatCtx);
    }

    // ─────────────────────────────
    // SPAWNING
    // ─────────────────────────────
    void SpawnFromContract()
    {
        if (enemyPrefab == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogError("[CombatRoom] Missing prefab or spawn points.");
            return;
        }

        aliveEnemies = 0;
        spawnIndex = 0;
        spawnedAgents.Clear();

        spawnGroupCount = 2;

        SpawnMany(EnemyRole.Offender, contract.offenders);
        SpawnMany(EnemyRole.Defender, contract.defenders);
        SpawnMany(EnemyRole.Ranger, contract.rangers);
        SpawnMany(EnemyRole.Activator, contract.activators);
        SpawnMany(EnemyRole.Joker, contract.jokers);

        Debug.Log($"[CombatRoom] Enemies spawned (tracked): {aliveEnemies}");

        var anchors = FindObjectsByType<SquadAnchor>(FindObjectsSortMode.None);
        if (anchors != null && anchors.Length > 0)
        {
            for (int i = 0; i < anchors.Length; i++)
                anchors[i].InitializeAfterSpawn();

            Debug.Log($"[CombatRoom] SquadAnchors initialized: {anchors.Length}. (Dormant until triggered)");
        }
        else
        {
            Debug.Log("[CombatRoom] No SquadAnchor found. Enemies remain dormant until activated.");
        }
    }


    void SpawnMany(EnemyRole role, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int groupIndex = spawnIndex % spawnGroupCount;

            Vector3 groupAnchor = GetGroupSpawnAnchor(groupIndex, spawnGroupCount);
            Vector3 jitter = UnityEngine.Random.insideUnitCircle * 0.6f;

            Transform sp = enemySpawnPoints[spawnIndex % enemySpawnPoints.Length];
            spawnIndex++;

            SpawnEnemy(role, groupAnchor + jitter);
        }
    }

    // ⭐ UPDATED: spawn directly at position
    void SpawnEnemy(EnemyRole role, Vector3 position)
    {
        Debug.Log($"[SquadAnchor] Scene = {gameObject.scene.name}");

        if (!RolePermissionBus.IsRoleAllowed(role))
        {
            Debug.Log($"[CombatRoom] Skipping spawn of {role} (not allowed)");
            return;
        }

        var go = Instantiate(enemyPrefab, position, Quaternion.identity);

        var roleCtrl = go.GetComponent<EnemyRoleController>();
        if (roleCtrl != null)
            roleCtrl.ApplyRole(role);

        var agent = go.GetComponent<EnemyAgent>();
        if (agent != null)
        {
            agent.role = role;
            spawnedAgents.Add(agent);
            aliveEnemies++;
        }

        
        Debug.Log($"[CombatRoom] Scene = {gameObject.scene.name}");

        var relay = go.AddComponent<EnemyDeathRelay>();
        relay.OnEnemyDestroyed = OnEnemyDestroyed;

    }

    // ─────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────
    public void NotifyEnemyRejected()
    {
        aliveEnemies--;
        Debug.Log($"[CombatRoom] Enemy rejected. Remaining: {aliveEnemies}");

        if (aliveEnemies <= 0)
            CompleteRoom();
    }

    void OnEnemyDestroyed()
    {
        if (roomCompletionTriggered)
            return;

        aliveEnemies--;

        Debug.Log($"[CombatRoom] Enemy destroyed. Remaining: {aliveEnemies}");

        if (aliveEnemies <= 0)
        {
            roomCompletionTriggered = true;
            CompleteRoom();
        }
    }

    private class EnemyDeathRelay : MonoBehaviour
    {
        public Action OnEnemyDestroyed;
        private bool quitting;

        void OnApplicationQuit() => quitting = true;

        void OnDestroy()
        {
            if (!Application.isPlaying || quitting)
                return;

            OnEnemyDestroyed?.Invoke();
        }
    }
}
