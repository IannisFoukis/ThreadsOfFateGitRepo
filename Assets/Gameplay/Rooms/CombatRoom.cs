using System.Collections.Generic;
using UnityEngine;
using static Shrine;

public class CombatRoom : RoomController
{
    
    bool roomCompleted;
    private GameStateManager gsm;
    private readonly List<GameObject> spawnedEnemies = new();
    private readonly List<GameObject> activeEnemies = new();

    [SerializeField] float escalationDelay = 8f;
    bool escalationTriggered;
    float escalationTimer;

    public GameObject enemyPrefab;
    public EncounterType encounterType;
    public Shrine shrine;


    

    protected override void Start()
    {
        base.Start();

        gsm = FindAnyObjectByType<GameStateManager>();
        if (gsm == null)
        {
            Debug.LogError("CombatRoom: GameStateManager not found!");
            return;
        }

        SpawnEncounter();

        if (shrine != null)
            shrine.Activate();
    }

    void Update()
    {
        if (Enemy.AliveCount <= 0)
        {
            if (ChoiceManager.Instance != null &&
                ChoiceManager.Instance.ChoicePending)
                return;
            Debug.Log(
       $"[RUN DEBUG] Room cleared | roomsCleared BEFORE = {gsm.RunData.roomsCleared}"
   );
            gsm.OnRoomCleared();
            Debug.Log(
        $"[RUN DEBUG] roomsCleared AFTER = {gsm.RunData.roomsCleared}");
            roomCompleted = true;
            CompleteRoom();
        }

        if (!roomCompleted)
        {
            escalationTimer += Time.deltaTime;

            if (!escalationTriggered &&
             (escalationTimer >= escalationDelay ||
             RunCorruptionState.Instance.Level >= 2))

            {
                TriggerMidFightEscalation();
                escalationTriggered = true;
            }
        }

    }
    void TriggerMidFightEscalation()
    {
        Debug.Log("[ESCALATION] Mid-fight escalation triggered");

        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            var behavior = enemy.GetComponent<EnemyBehaviorController>();
            if (behavior == null) continue;

            if (behavior.currentTier == EnemyBehaviorTier.Base)
                behavior.ApplyTier(EnemyBehaviorTier.Aggressive);
            else if (behavior.currentTier == EnemyBehaviorTier.Aggressive)
                behavior.ApplyTier(EnemyBehaviorTier.Elite);

            Debug.Log($"[ESCALATION] {enemy.name} → {behavior.currentTier}");
        }
    }


    void SpawnEncounter()
    {
        switch (encounterType)
        {
            case EncounterType.Skirmish:
                Spawn(EnemyRole.Melee, 2);
                break;

            case EncounterType.Crossfire:
                Spawn(EnemyRole.Ranged, 2);
                Spawn(EnemyRole.Melee, 1);
                break;

            case EncounterType.BurstThreat:
                Spawn(EnemyRole.Charger, 1);
                Spawn(EnemyRole.Melee, 1);
                break;

            case EncounterType.Mixed:
                Spawn(EnemyRole.Melee, 1);
                Spawn(EnemyRole.Ranged, 1);
                Spawn(EnemyRole.Charger, 1);
                break;

            case EncounterType.Lockdown:
                Spawn(EnemyRole.Melee, 2);
                Spawn(EnemyRole.Ranged, 2);
                Spawn(EnemyRole.Charger, 1);
                break;
        }

        TryAssignJoker();
        TrySpawnElite();
    }

    void Spawn(EnemyRole role, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 3f;

            GameObject enemy = Instantiate(
                enemyPrefab,
                transform.position + (Vector3)offset,
                Quaternion.identity
            );

            ConfigureEnemy(enemy, role);
            ApplyEnemyScaling(enemy);
            CorruptionEffects.Apply(enemy);
            ApplyShrineScaling(enemy);
            ApplyBehaviorEscalation(enemy);
            
            spawnedEnemies.Add(enemy);
            activeEnemies.Add(enemy);
        }
    }

    void ConfigureEnemy(GameObject enemy, EnemyRole role)
    {
        enemy.GetComponent<EnemyRoleController>().role = role;

        enemy.GetComponent<EnemyMelee>().enabled = role == EnemyRole.Melee;
        enemy.GetComponent<EnemyRanged>().enabled = role == EnemyRole.Ranged;
        enemy.GetComponent<EnemyCharger>().enabled = role == EnemyRole.Charger;
    }

    // -------------------------
    // ENEMY SCALING
    // -------------------------
    void ApplyBehaviorEscalation(GameObject enemy)
    {
        var controller = enemy.GetComponent<EnemyBehaviorController>();
        if (controller == null) return;

        Shrine activeShrine = shrine != null ? shrine : FindAnyObjectByType<Shrine>();
        RunState run = gsm?.RunState;

        EnemyBehaviorTier targetTier = EnemyBehaviorTier.Base;

        if ((activeShrine != null && activeShrine.currentTier == ShrineTier.Tier3) ||
            (run != null && run.runTension >= 7))
        {
            targetTier = EnemyBehaviorTier.Elite;
        }
        else if ((activeShrine != null && activeShrine.currentTier == ShrineTier.Tier2) ||
                 (run != null && run.runTension >= 4))
        {
            targetTier = EnemyBehaviorTier.Aggressive;
        }

        controller.ApplyTier(targetTier);
        Debug.Log($"[ENEMY] Behavior tier = {controller.currentTier}");

        // 🔥 CORRUPTION APPLIED HERE
        if (RunCorruptionState.Instance.IsCorrupted)
            controller.ApplyCorruption(RunCorruptionState.Instance.Level);

        Debug.Log($"[ENEMY] Tier={controller.currentTier} | Corruption={RunCorruptionState.Instance.Level}");

    }




    void ApplyEnemyScaling(GameObject enemy)
    {
        var runData = gsm.RunData;

        if (gsm == null) return;

        var run = gsm.RunData;

        var health = enemy.GetComponent<Health>();
        var damage = enemy.GetComponent<DamageOnContact>();

        if (health != null)
        {

            int before = health.maxHealth;


            float hpMult = 1f + run.roomsCleared * 0.15f;
            health.ScaleMaxHealth(hpMult);

            Debug.Log(
        $"[SCALING] Enemy HP scaled: {before} → {health.maxHealth} " +
        $"(roomsCleared={runData.roomsCleared}, mult={hpMult:F2})"
    );


        }

        if (damage != null)
        {
            float dmgMult = 1f + runData.tension * 0.10f;

            damage.damageMultiplier = 1f + run.tension * 0.10f;

            Debug.Log(
        $"[SCALING] Enemy damage multiplier = {dmgMult:F2} " +
        $"(tension={runData.tension})"
    );
        }
    }

    // -------------------------
    // ELITE LOGIC
    // -------------------------

    void TrySpawnElite()
    {
        if (EliteSpawner.Instance == null || gsm == null) return;

        var run = gsm.RunData;

        bool eliteCondition =
            run.roomsCleared % 3 == 0 ||
            run.tension >= 5 ||
            run.corruption >= 4;

        if (eliteCondition)
        {
            EliteSpawner.Instance.SpawnElite();
            Debug.Log("ELITE PRESSURE APPLIED");
        }
    }

    // -------------------------
    // JOKER
    // -------------------------

    void TryAssignJoker()
    {
        if (spawnedEnemies.Count == 0) return;
        if (JokerManager.Instance == null) return;
        if (Random.value > 0.5f) return;

        int index = Random.Range(0, spawnedEnemies.Count);
        GameObject enemy = spawnedEnemies[index];

        JokerManager.Instance.AssignJoker(enemy);
        Debug.Log("JOKER ASSIGNED TO: " + enemy.name);
    }

    void ApplyShrineScaling(GameObject enemy)
    {
        Shrine shrine = FindAnyObjectByType<Shrine>();
        if (shrine == null) return;

        var health = enemy.GetComponent<Health>();
        var damage = enemy.GetComponent<DamageOnContact>();

        if (health != null)
            health.ScaleMaxHealth(shrine.GetEnemyHpMultiplier());

        if (damage != null)
            damage.damageMultiplier *= shrine.GetEnemyDamageMultiplier();

        Debug.Log($"[SHRINE] Enemy scaled: HP x{shrine.GetEnemyHpMultiplier()}, DMG x{shrine.GetEnemyDamageMultiplier()}");
    }

}
