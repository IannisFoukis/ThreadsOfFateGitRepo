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


    [Header("Doctrine (Debug)")]
    [SerializeField] private RoomDoctrine selectedDoctrine;
    [SerializeField] private bool randomizeDoctrine = true;

    [Header("Prefab + Spawn Points")]
    public GameObject enemyPrefab;
    public Transform[] enemySpawnPoints;

    private TacticDirector tacticDirector;
    private EncounterCoordinator encounterCoordinator;

    private int aliveEnemies;

    [Header("Test Spawn Settings")]
    public int offenders = 5;
    public int defenders = 3;
    public int rangers = 2;
    public int activators = 0;
    public int jokers = 0;

    private void Awake()
    {
        tacticDirector = GetComponent<TacticDirector>();
        encounterCoordinator = GetComponentInChildren<EncounterCoordinator>();

        if (coordinationMode == CombatCoordinationMode.Legacy && tacticDirector == null)
            Debug.LogError("[CombatRoom] TacticDirector missing on CombatRoom!");

        if (coordinationMode == CombatCoordinationMode.EnemyCentric && encounterCoordinator == null)
            Debug.LogError("[CombatRoom] EncounterCoordinator missing on CombatRoom!");

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
        if (coordinationMode == CombatCoordinationMode.Legacy)
        {
            if (randomizeDoctrine)
                selectedDoctrine = RollDoctrine();

            Debug.Log($"[CombatRoom] Selected Doctrine: {selectedDoctrine}");
            tacticDirector.ConfigureDoctrine(selectedDoctrine);
        }
        else
        {
            Debug.Log("[CombatRoom] Using Enemy-Centric coordination – doctrine handled by EncounterCoordinator");
        }
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
                var sp = enemySpawnPoints[spawnIndex % enemySpawnPoints.Length];
                SpawnEnemy(role, sp);
                spawnIndex++;
            }
        }

        SpawnMany(EnemyRole.Melee, offenders);
        SpawnMany(EnemyRole.Elite, defenders);
        SpawnMany(EnemyRole.Ranged, rangers);
        SpawnMany(EnemyRole.Activator, activators);
        SpawnMany(EnemyRole.Joker, jokers);

        Debug.Log("[CombatRoom] Test encounter spawned via role counts");
    }


    private void SpawnEnemy(EnemyRole role, Transform sp)
    {
        var go = Instantiate(enemyPrefab, sp.position, Quaternion.identity);

        var enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            aliveEnemies++;

            // In BOTH systems we simply use EnemyRoleController
            var roleCtrl = go.GetComponent<EnemyRoleController>();
            if (roleCtrl != null)
            {
                roleCtrl.ApplyRole(role);
            }

            // Legacy tactical director only used in legacy mode
            if (coordinationMode == CombatCoordinationMode.Legacy)
            {
                tacticDirector.Register(enemy);
            }

            // NEW SYSTEM: nothing else is required
            // EncounterCoordinator + EnemyAgent handle everything automatically

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
