using System.Collections.Generic;
using UnityEngine;
using static Shrine;

public class CombatRoom : RoomController
{
    public GameObject enemyPrefab;
    public EncounterType encounterType;
    public Shrine shrine;

    private GameStateManager gsm;
    private readonly List<GameObject> spawnedEnemies = new();

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
            CompleteRoom();
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
        Debug.Log("[BEHAVIOR] ApplyBehaviorEscalation CALLED");

        var controller = enemy.GetComponent<EnemyBehaviorController>();
        if (controller == null)
        {
            Debug.Log("[BEHAVIOR] No EnemyBehaviorController found");
            return;
        }

        if (gsm == null)
        {
            Debug.Log("[BEHAVIOR] gsm is NULL → Base tier");
            controller.ApplyTier(EnemyBehaviorTier.Base);
            
            return;
        }

        // Prefer the room's assigned shrine reference if you have it
        Shrine activeShrine = shrine != null ? shrine : FindAnyObjectByType<Shrine>();
        if (activeShrine == null)
        {
            Debug.Log("[BEHAVIOR] gsm is NULL → Base tier");
            controller.ApplyTier(EnemyBehaviorTier.Base);
           
            return;
        }

        RunState run = gsm.RunState;
        if (run == null)
        {
            Debug.Log("[BEHAVIOR] gsm is NULL → Base tier");
            controller.ApplyTier(EnemyBehaviorTier.Base);
            
            return;
        }

        EnemyBehaviorTier targetTier = EnemyBehaviorTier.Base;

        if (activeShrine.currentTier == ShrineTier.Tier2 || run.runTension >= 4)
        {
            targetTier = EnemyBehaviorTier.Aggressive;
            return;

        }



        if (activeShrine.currentTier == ShrineTier.Tier3 || run.runTension >= 7)
        {
            targetTier = EnemyBehaviorTier.Elite;
            return;
        }

        controller.ApplyTier(targetTier);

        Debug.Log($"[ENEMY] Behavior tier = {controller.currentTier}");
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
