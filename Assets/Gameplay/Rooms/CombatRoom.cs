using UnityEngine;
using System;

public class CombatRoom : RoomController
{
    public enum CombatCoordinationMode
    {
        Legacy,
        EnemyCentric
    }

    [Header("Coordination System")]
    [SerializeField] private CombatCoordinationMode coordinationMode = CombatCoordinationMode.EnemyCentric;

    [Header("Prefab + Spawn Points")]
    public GameObject enemyPrefab;
    public Transform[] enemySpawnPoints;

    [Header("Test Spawn Settings")]
    public int offenders = 6;
    public int defenders = 3;
    public int rangers = 4;
    public int activators = 0;
    public int jokers = 0;

    private int aliveEnemies;

    protected override void Start()
    {
        base.Start();
        SpawnEncounter();
    }

    // ─────────────────────────────────────────────
    // SPAWNING
    // ─────────────────────────────────────────────

    private void SpawnEncounter()
    {
        if (enemyPrefab == null || enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogError("[CombatRoom] Missing prefab or spawn points.");
            return;
        }

        aliveEnemies = 0;
        int spawnIndex = 0;

        void SpawnMany(EnemyRole role, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Transform sp = enemySpawnPoints[spawnIndex % enemySpawnPoints.Length];
                spawnIndex++;
                SpawnEnemy(role, sp);
            }
        }

        SpawnMany(EnemyRole.Melee, offenders);
        SpawnMany(EnemyRole.Elite, defenders);
        SpawnMany(EnemyRole.Ranged, rangers);
        SpawnMany(EnemyRole.Activator, activators);
        SpawnMany(EnemyRole.Joker, jokers);

        Debug.Log("[CombatRoom] Encounter spawned via role counts");
    }

    private void SpawnEnemy(EnemyRole role, Transform sp)
    {
        var go = Instantiate(enemyPrefab, sp.position, Quaternion.identity);
        aliveEnemies++;

        var roleCtrl = go.GetComponent<EnemyRoleController>();
        if (roleCtrl != null)
            roleCtrl.ApplyRole(role);

        var agent = go.GetComponent<EnemyAgent>();
        if (agent != null)
        {
            agent.role = role switch
            {
                EnemyRole.Melee => EnemyRole.Offender,
                EnemyRole.Ranged => EnemyRole.Ranger,
                EnemyRole.Elite => EnemyRole.Defender,
                EnemyRole.Activator => EnemyRole.Activator,
                EnemyRole.Joker => EnemyRole.Joker,
                _ => EnemyRole.Offender
            };
        }

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
        {
            CompleteRoom(); // 🔒 SINGLE AUTHORITY PATH
        }
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
