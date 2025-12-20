using UnityEngine;
using System.Collections.Generic;

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
}
