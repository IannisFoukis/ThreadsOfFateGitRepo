using UnityEngine;
using System;

public class CombatRoom : MonoBehaviour
{
    [Header("Doctrine (Debug)")]
    [SerializeField] private RoomDoctrine selectedDoctrine;
    [SerializeField] private bool randomizeDoctrine = true;

    [Header("Prefab + Spawn Points")]
    public GameObject enemyPrefab;
    public Transform[] enemySpawnPoints;

    private TacticDirector tacticDirector;
    private int aliveEnemies;

    private void Awake()
    {
        tacticDirector = GetComponent<TacticDirector>();
        if (tacticDirector == null)
            Debug.LogError("[CombatRoom] TacticDirector missing on CombatRoom!");
    }

    private void Start()
    {
        ApplyDoctrine();
        SpawnEncounter();
    }

    // ─────────────────────────────────────────────────────
    // DOCTRINE
    // ─────────────────────────────────────────────────────

    private void ApplyDoctrine()
    {
        if (randomizeDoctrine)
            selectedDoctrine = RollDoctrine();

        Debug.Log($"[CombatRoom] Selected Doctrine: {selectedDoctrine}");
        tacticDirector.ConfigureDoctrine(selectedDoctrine);
    }

    private RoomDoctrine RollDoctrine()
    {
        var values = Enum.GetValues(typeof(RoomDoctrine));
        return (RoomDoctrine)values.GetValue(
            UnityEngine.Random.Range(0, values.Length)
        );
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

        if (enemySpawnPoints == null || enemySpawnPoints.Length < 3)
        {
            Debug.LogError("[CombatRoom] Need at least 3 enemySpawnPoints.");
            return;
        }

        aliveEnemies = 0;

        SpawnEnemy(EnemyRole.Melee, enemySpawnPoints[0]);
        SpawnEnemy(EnemyRole.Ranged, enemySpawnPoints[1]);
        SpawnEnemy(EnemyRole.Melee, enemySpawnPoints[2]);

        if (enemySpawnPoints.Length > 3 && UnityEngine.Random.value < 0.35f)
            SpawnEnemy(EnemyRole.Elite, enemySpawnPoints[3]);

        if (enemySpawnPoints.Length > 4 && UnityEngine.Random.value < 0.2f)
            SpawnEnemy(EnemyRole.Joker, enemySpawnPoints[4]);

        Debug.Log("[CombatRoom] Mixed encounter spawned");
    }

    private void SpawnEnemy(EnemyRole role, Transform sp)
    {
        var go = Instantiate(enemyPrefab, sp.position, Quaternion.identity);

        var roleCtrl = go.GetComponent<EnemyRoleController>();
        if (roleCtrl != null)
            roleCtrl.ApplyRole(role);

        var enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            aliveEnemies++;
            tacticDirector.Register(enemy);

            var relay = go.AddComponent<EnemyDeathRelay>();
            relay.OnEnemyDestroyed = OnEnemyDestroyed;
        }

        Debug.Log($"[CombatRoom] Spawned {role} at {go.transform.position}");
    }

    // ─────────────────────────────────────────────────────
    // COMBAT LIFECYCLE
    // ─────────────────────────────────────────────────────

    private void OnEnemyDestroyed()
    {
        aliveEnemies--;

        Debug.Log($"[CombatRoom] Enemy died. Remaining: {aliveEnemies}");

        if (aliveEnemies <= 0)
        {
            Debug.Log("[CombatRoom] Combat cleared");
            CompleteRoom();
        }
    }

    private void CompleteRoom()
    {
        var runDirector = FindAnyObjectByType<RunDirector>();

        if (runDirector == null || !runDirector.IsRunActive)
        {
            Debug.Log("[CombatRoom] Combat cleared, but run is no longer active. Ignoring.");
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
