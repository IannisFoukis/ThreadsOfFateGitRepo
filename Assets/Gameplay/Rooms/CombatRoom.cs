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
        Debug.Log("[CombatRoom] Combat started");
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

        Debug.Log($"[CombatRoom] Enemies spawned: {aliveEnemies}");
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
        var go = Instantiate(enemyPrefab, sp.position, Quaternion.identity);
        aliveEnemies++;

        var roleCtrl = go.GetComponent<EnemyRoleController>();
        if (roleCtrl != null)
            roleCtrl.ApplyRole(role);

        var agent = go.GetComponent<EnemyAgent>();
        if (agent != null)
            agent.role = role;

        var relay = go.AddComponent<EnemyDeathRelay>();
        relay.OnEnemyDestroyed = OnEnemyDestroyed;
    }

    void OnEnemyDestroyed()
    {
        if (shuttingDown)
            return;

        aliveEnemies--;

        if (aliveEnemies <= 0)
        {
            shuttingDown = true;
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