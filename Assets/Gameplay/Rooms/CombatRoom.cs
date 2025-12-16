using UnityEngine;
using System.Collections.Generic;

public class CombatRoom : RoomController
{
    public GameObject enemyPrefab;
    public EncounterType encounterType;

    private readonly List<GameObject> spawnedEnemies = new();

    protected override void Start()
    {
        base.Start();
        SpawnEncounter();
    }

    void Update()
    {
        if (Enemy.AliveCount <= 0)
        {
            if (ChoiceManager.Instance != null &&
                ChoiceManager.Instance.ChoicePending)
                return;

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
    }

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
}
