using UnityEngine;
using System;

public class CombatRoom : RoomController
{
    [Header("Prefab + Spawn Points")]
    public GameObject enemyPrefab;
    public Transform[] enemySpawnPoints;

    [Header("Debug / Safety")]
    public bool applyFallbackIfZeroCounts = true; // turn off later

    private int aliveEnemies;
    private RoomContract contract;
    private int spawnIndex;
    private bool shuttingDown;

    protected override void Start()
    {
        base.Start();

        contract = RoomAccess.Current;

        if (contract == null)
        {
            Debug.LogError("[CombatRoom] RoomAccess.Current is NULL (no contract).");
            return;
        }

        Debug.Log($"[CombatRoom] Contract='{contract.contractName}' Role={contract.roomRole} enableCombat={contract.enableCombat}");

        if (!contract.enableCombat)
        {
            Debug.Log("[CombatRoom] Combat disabled by contract");
            return;
        }

        Debug.Log(
            $"[CombatRoom] Counts → " +
            $"Off:{contract.offenders} Def:{contract.defenders} Ran:{contract.rangers} " +
            $"Act:{contract.activators} Jok:{contract.jokers}"
        );

        int total =
            contract.offenders +
            contract.defenders +
            contract.rangers +
            contract.activators +
            contract.jokers;

        if (total <= 0)
        {
            Debug.LogWarning("[CombatRoom] Contract has ZERO enemy counts. Nothing will spawn.");

            if (applyFallbackIfZeroCounts)
            {
                // DEV fallback so you can keep testing rooms even if a contract wasn't authored yet
                contract.offenders = 3;
                contract.defenders = 1;
                contract.rangers = 1;
                Debug.LogWarning("[CombatRoom] Applied fallback counts: Off=3 Def=1 Ran=1");
            }
            else
            {
                return;
            }
        }

        SpawnFromContract();
    }

    // ─────────────────────────────────────────────
    // CONTRACT-DRIVEN SPAWN (G1)
    // ─────────────────────────────────────────────

    private void SpawnFromContract()
    {
        if (enemyPrefab == null || enemySpawnPoints == null || enemySpawnPoints.Length == 0)
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

        Debug.Log($"[CombatRoom] Spawned encounter from RoomContract | Alive={aliveEnemies}");
    }

    private void SpawnMany(EnemyRole role, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Transform sp = enemySpawnPoints[spawnIndex % enemySpawnPoints.Length];
            spawnIndex++;
            SpawnEnemy(role, sp);
        }
    }

    private void SpawnEnemy(EnemyRole role, Transform sp)
    {
        var go = Instantiate(enemyPrefab, sp.position, Quaternion.identity);
        aliveEnemies++;

        var roleCtrl = go.GetComponent<EnemyRoleController>();
        if (roleCtrl != null)
            roleCtrl.ApplyRole(role);

        var enemyAgent = go.GetComponent<EnemyAgent>();
        if (enemyAgent != null)
            enemyAgent.role = role;

        // G1: Elite intent only (no behavior yet)
        if (contract.allowElites && UnityEngine.Random.value < contract.eliteChance)
        {
            go.name += " [ELITE]";
        }

        var relay = go.AddComponent<EnemyDeathRelay>();
        relay.OnEnemyDestroyed = OnEnemyDestroyed;
    }

    // ─────────────────────────────────────────────
    // COMBAT LIFECYCLE
    // ─────────────────────────────────────────────

    private void OnEnemyDestroyed()
    {
        if (shuttingDown)
            return;

        aliveEnemies--;

        Debug.Log($"[CombatRoom] Enemy died. Remaining: {aliveEnemies}");

        if (aliveEnemies <= 0)
        {
            shuttingDown = true;
            CompleteRoom();
        }
    }


    // ─────────────────────────────────────────────
    // DEATH RELAY
    // ─────────────────────────────────────────────

    private class EnemyDeathRelay : MonoBehaviour
    {
        public Action OnEnemyDestroyed;
        private bool quitting;

        private void OnApplicationQuit()
        {
            quitting = true;
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying || quitting)
                return;

            if (OnEnemyDestroyed == null)
                return;

            OnEnemyDestroyed.Invoke();
        }
    }

}
