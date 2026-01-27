using UnityEngine;
using System;

public class CombatRoom : MonoBehaviour
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
    private bool completed = false; // 🔒 one-shot guard

    private void Start()
    {
        SpawnEncounter();
    }

    // ─────────────────────────────────────────────────────
    // SPAWNING
    // ─────────────────────────────────────────────────────

    private void SpawnEncounter()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[CombatRoom] Missing enemyPrefab.");
            return;
        }

        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogError("[CombatRoom] No spawn points configured.");
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

                if (sp == null)
                {
                    Debug.LogWarning("[CombatRoom] Spawn point missing — skipped");
                    continue;
                }

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

        // Apply visual / legacy role handling
        var roleCtrl = go.GetComponent<EnemyRoleController>();
        if (roleCtrl != null)
            roleCtrl.ApplyRole(role);

        // Enemy-centric coordination
        var agent = go.GetComponent<EnemyAgent>();
        if (agent != null)
        {
            agent.role = role switch
            {
                EnemyRole.Melee => EnemyRole.Offender,
                EnemyRole.Ranged => EnemyRole.Ranger,
                EnemyRole.Elite => EnemyRole.Defender,
                EnemyRole.Joker => EnemyRole.Joker,
                EnemyRole.Activator => EnemyRole.Activator,
                _ => EnemyRole.Offender
            };
        }

        var relay = go.AddComponent<EnemyDeathRelay>();
        relay.OnEnemyDestroyed = OnEnemyDestroyed;

        Debug.Log($"[CombatRoom] Spawned {role} at {go.transform.position}");
    }

    // ─────────────────────────────────────────────────────
    // COMBAT LIFECYCLE
    // ─────────────────────────────────────────────────────

    private void OnEnemyDestroyed()
    {
        if (completed)
            return;

        aliveEnemies--;
        Debug.Log($"[CombatRoom] Enemy died. Remaining: {aliveEnemies}");

        if (aliveEnemies <= 0)
        {
            completed = true;
            CompleteRoom();
        }
    }

    private void CompleteRoom()
    {
        var runDirector = FindAnyObjectByType<RunDirector>();

        if (runDirector == null)
        {
            Debug.LogError("[CombatRoom] RunDirector missing — aborting completion");
            return;
        }

        Debug.Log("[CombatRoom] Combat cleared → notifying RunDirector");
        runDirector.OnCombatRoomCleared();
    }

    // ─────────────────────────────────────────────────────
    // DEATH RELAY
    // ─────────────────────────────────────────────────────

    private class EnemyDeathRelay : MonoBehaviour
    {
        public Action OnEnemyDestroyed;

        private void OnDestroy()
        {
            if (!Application.isPlaying)
                return;

            OnEnemyDestroyed?.Invoke();
        }
    }
}
