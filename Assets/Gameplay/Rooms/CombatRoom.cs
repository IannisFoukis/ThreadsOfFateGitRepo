using UnityEngine;
using System;
using TOF.Rooms.Contracts;

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

        int total =
            contract.offenders +
            contract.defenders +
            contract.rangers +
            contract.activators +
            contract.jokers;

        if (total <= 0 && applyFallbackIfZeroCounts)
        {
            contract.offenders = 3;
            contract.defenders = 1;
            contract.rangers = 1;
        }

        SpawnFromContract();

        // --- Initialize combat context ---
        combatTimer = 0f;

        combatCtx = new CombatRoomContext
        {
            roomContext = this.roomContext, // inherited from RoomController
            timeInRoom = 0f,
            enemiesAliveRatio = 1f,
            formationIntegrity = 1f,   // start cohesive
            playerPressure = 0f,

            shrinePresent = false,     // can be wired later
            shrineActive = false,
            playerLowHealth = false,

            difficultyTier = 0
        };

        if (laneDirector != null)
            laneDirector.SetContext(combatCtx);

        Debug.Log($"[CombatRoom] Combat started | Alive={aliveEnemies}");
    }
    void Update()
    {
        if (!contract.enableCombat)
            return;

        combatTimer += Time.deltaTime;

        combatCtx.timeInRoom = combatTimer;

        // Keep this simple for now
        combatCtx.enemiesAliveRatio =
            aliveEnemies > 0 ? aliveEnemies / (float)(
                contract.offenders +
                contract.defenders +
                contract.rangers +
                contract.activators +
                contract.jokers
            ) : 0f;

        // TEMP defaults (we will wire these later)
        combatCtx.playerPressure = 0.5f;
        combatCtx.formationIntegrity = 1f;

        if (laneDirector != null)
            laneDirector.SetContext(combatCtx);
    }
    void SpawnFromContract()
    {
        if (enemyPrefab == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogError("[CombatRoom] Missing prefab or spawn points.");
            return;
        }

        aliveEnemies = 0;
        spawnIndex = 0;

        SpawnMany(EnemyRole.Offender, contract.offenders);
        SpawnMany(EnemyRole.Defender, contract.defenders);
        SpawnMany(EnemyRole.Ranger, contract.rangers);
        SpawnMany(EnemyRole.Activator, contract.activators);
        SpawnMany(EnemyRole.Joker, contract.jokers);

        Debug.Log($"[CombatRoom] Enemies spawned (active combatants): {aliveEnemies}");
    }

    void SpawnMany(EnemyRole role, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Transform sp = enemySpawnPoints[spawnIndex % enemySpawnPoints.Length];
            spawnIndex++;
            SpawnEnemy(role, sp);
        }
    }

    void SpawnEnemy(EnemyRole role, Transform sp)
    {
        // 🔒 ROLE FILTERING HAPPENS HERE
        if (!RolePermissionBus.IsRoleAllowed(role))
        {
            Debug.Log($"[CombatRoom] Skipping spawn of {role} (not allowed in this room)");
            return;
        }

        var go = Instantiate(enemyPrefab, sp.position, Quaternion.identity);

        var roleCtrl = go.GetComponent<EnemyRoleController>();
        if (roleCtrl != null)
            roleCtrl.ApplyRole(role);

        var agent = go.GetComponent<EnemyAgent>();
        if (agent != null)
            agent.role = role;

        // ✅ COUNT ONLY REAL COMBATANTS
        aliveEnemies++;

        var relay = go.AddComponent<EnemyDeathRelay>();
        relay.OnEnemyDestroyed = OnEnemyDestroyed;
    }
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