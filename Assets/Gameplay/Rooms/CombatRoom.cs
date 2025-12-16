using UnityEngine;
using System.Collections.Generic;

public class CombatRoom : RoomController
{
    public GameObject enemyPrefab;
    public EncounterType encounterType;

    protected override void Start()
    {
        base.Start();
        SpawnEncounter();
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
