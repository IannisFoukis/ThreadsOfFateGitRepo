using System.Collections.Generic;
using UnityEngine;
using static Shrine;

public class CombatRoom : RoomController
{
    bool tensionSpike4Triggered;
    bool tensionSpike7Triggered;
    bool tensionSpike10Triggered;

    bool roomCompleted;

    GameStateManager gsm;

    readonly List<GameObject> spawnedEnemies = new();
    readonly List<GameObject> activeEnemies = new();

    [Header("Encounter")]
    public GameObject enemyPrefab;
    public EncounterType encounterType;
    public Shrine shrine;

    [Header("Mid-Fight Escalation")]
    [SerializeField] float escalationDelay = 8f;
    float escalationTimer;
    bool escalationTriggered;

    // Reserved for future lockdown mechanics; currently only tracks whether the room
    // is in a high-corruption state.
#pragma warning disable CS0414
    bool lockdownActive;
#pragma warning restore CS0414

    float clearConfirmTimer = 0f;
    const float clearConfirmThreshold = 0.25f;
    bool clearConfirming = false;

    protected override void Start()
    {
        base.Start();

        gsm = FindAnyObjectByType<GameStateManager>();
        if (gsm == null)
        {
            Debug.LogError("CombatRoom: GameStateManager not found");
            return;
        }

        GameEvents.OnEnemyRemoved += OnEnemyRemoved;

        ApplyCorruptedRoomRules();

        if (shrine != null)
            shrine.PrepareForRoom();

        SpawnEncounter();
    }

    void OnDestroy()
    {
        GameEvents.OnEnemyRemoved -= OnEnemyRemoved;
    }

    void OnEnemyRemoved(GameObject enemy)
    {
        if (enemy == null) return;
        activeEnemies.Remove(enemy);
    }

    void Update()
    {
        if (roomCompleted)
            return;

        int living = 0;

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;
            if (!enemy.activeInHierarchy) continue;

            var h = enemy.GetComponent<Health>();
            if (h == null || h.currentHealth > 0)
                living++;
        }

        if (living <= 0)
        {
            if (ChoiceManager.Instance != null &&
                ChoiceManager.Instance.ChoicePending)
                return;

            if (!clearConfirming)
            {
                clearConfirming = true;
                clearConfirmTimer = 0f;
                return;
            }

            clearConfirmTimer += Time.deltaTime;
            if (clearConfirmTimer < clearConfirmThreshold)
                return;

            roomCompleted = true;
            gsm.OnRoomCleared();
            GameEvents.RaiseRoomCompleted();
            CompleteRoom();
            return;
        }
        else
        {
            clearConfirming = false;
            clearConfirmTimer = 0f;
        }

        CheckTensionSpikes();

        escalationTimer += Time.deltaTime;
        if (!escalationTriggered &&
            (escalationTimer >= escalationDelay ||
             RunCorruptionState.Instance.Level >= 2))
        {
            TriggerMidFightEscalation();
            escalationTriggered = true;
        }
    }

    // ============================
    // ENCOUNTERS
    // ============================

    void SpawnEncounter()
    {
        bool coordinated =
    RunContext.Instance != null &&
    RunContext.Instance.rules.enemiesCoordinateMore;

        Debug.Log($"[CombatRoom] Coordinated enemies: {coordinated}");

        switch (encounterType)
        {
            case EncounterType.Skirmish:
                Spawn(EnemyRole.Melee, 2);
                break;
            case EncounterType.Crossfire:
                Spawn(EnemyRole.Defender, 2);
                Spawn(EnemyRole.Melee, 1);
                break;
            case EncounterType.BurstThreat:
                Spawn(EnemyRole.Charger, 1);
                Spawn(EnemyRole.Melee, 1);
                break;
            case EncounterType.Mixed:
                Spawn(EnemyRole.Melee, 1);
                Spawn(EnemyRole.Defender, 1);
                Spawn(EnemyRole.Charger, 1);
                break;
            case EncounterType.Lockdown:
                Spawn(EnemyRole.Melee, 2);
                Spawn(EnemyRole.Defender, 2);
                Spawn(EnemyRole.Charger, 1);
                break;
        }

        TryAssignJoker();
    }

    void Spawn(EnemyRole baseRole, int count)
    {
        bool coordinated =
            RunContext.Instance != null &&
            RunContext.Instance.rules.enemiesCoordinateMore;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 3f;
            GameObject enemy = Instantiate(
                enemyPrefab,
                transform.position + (Vector3)offset,
                Quaternion.identity
            );

            EnemyRole finalRole = baseRole;

            // 🔥 OPTION A: coordination bias
            if (coordinated)
            {
                if (i == 0)
                {
                    finalRole = EnemyRole.Melee; // pressure
                }
                else if (baseRole == EnemyRole.Melee)
                {
                    finalRole = EnemyRole.Defender; // hangs back
                }
            }

            ConfigureEnemy(enemy, finalRole);

            Debug.Log($"[CombatRoom] Spawned enemy with role: {finalRole}");

            GameEvents.RaiseEnemySpawned(enemy);

            spawnedEnemies.Add(enemy);
            activeEnemies.Add(enemy);
        }
    }


    void ConfigureEnemy(GameObject enemy, EnemyRole role)
    {
        var roleController = enemy.GetComponent<EnemyRoleController>();
        if (roleController != null)
            roleController.role = role;

        var enemyComp = enemy.GetComponent<Enemy>();
        if (enemyComp != null)
            enemyComp.ApplyRole(role);
    }

    // ============================
    // JOKER
    // ============================

    void TryAssignJoker()
    {
        if (spawnedEnemies.Count == 0) return;
        if (JokerManager.Instance == null) return;
        if (Random.value > 0.5f) return;

        GameObject enemy = spawnedEnemies[Random.Range(0, spawnedEnemies.Count)];
        JokerManager.Instance.AssignJoker(enemy);
    }

    // ============================
    // ESCALATION
    // ============================

    void ApplyCorruptedRoomRules()
    {
        int corruption = RunCorruptionState.Instance.Level;

        if (corruption >= 1)
            Enemy.ForceImmediateAggro();

        if (corruption >= 2)
        {
            escalationDelay *= 0.5f;
            lockdownActive = true;
        }

        if (corruption >= 3 && shrine != null)
            shrine.ForceHazards();
    }

    void TriggerMidFightEscalation()
    {
        GameEvents.RaiseMidFightEscalation();

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            var behavior = enemy.GetComponent<EnemyBehaviorController>();
            if (behavior == null) continue;

            behavior.ApplyTier(
                behavior.currentTier == EnemyBehaviorTier.Base
                    ? EnemyBehaviorTier.Aggressive
                    : EnemyBehaviorTier.Elite
            );
        }
    }

    void CheckTensionSpikes()
    {
        RunState run = gsm.RunState;

        if (run.runTension >= 4 && !tensionSpike4Triggered)
        {
            tensionSpike4Triggered = true;
            Enemy.ForceImmediateAggro(2.5f);
        }

        if (run.runTension >= 7 && !tensionSpike7Triggered)
        {
            tensionSpike7Triggered = true;
            Enemy.ForceImmediateAggro(4f);
        }

        if (run.runTension >= 10 && !tensionSpike10Triggered)
        {
            tensionSpike10Triggered = true;
            Enemy.ForceImmediateAggro(6f);
        }
    }
}
