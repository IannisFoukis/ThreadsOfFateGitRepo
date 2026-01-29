using UnityEngine;
using System;

public class CombatRoom : RoomController
{
    [Header("Prefab + Spawn Points")]
    public GameObject enemyPrefab;
    public Transform[] enemySpawnPoints;

    private int aliveEnemies;
    private RoomContract contract;
    private int spawnIndex;

    protected override void Start()
    {
        base.Start();

        contract = RoomAccess.Current;

        if (contract == null || !contract.enableCombat)
        {
            Debug.Log("[CombatRoom] Combat disabled by contract");
            return;
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

        Debug.Log("[CombatRoom] Spawned encounter from RoomContract (G1)");
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

            go.name += " [ELITE]";

        var relay = go.AddComponent<EnemyDeathRelay>();
        relay.OnEnemyDestroyed = OnEnemyDestroyed;
    }

    // ─────────────────────────────────────────────
    // COMBAT LIFECYCLE
    // ─────────────────────────────────────────────

    private void OnEnemyDestroyed()
    {
        aliveEnemies--;
        Debug.Log($"[CombatRoom] Enemy died. Remaining: {aliveEnemies}");

        if (aliveEnemies <= 0)
            CompleteRoom();
    }

    // ─────────────────────────────────────────────
    // DEATH RELAY
    // ─────────────────────────────────────────────

    private class EnemyDeathRelay : MonoBehaviour
    {
        public Action OnEnemyDestroyed;

        private void OnDestroy()
        {
            if (Application.isPlaying)
                OnEnemyDestroyed?.Invoke();
        }
    }
}
