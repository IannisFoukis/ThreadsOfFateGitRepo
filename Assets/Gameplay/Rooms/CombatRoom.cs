using UnityEngine;

public class CombatRoom : RoomController
{
    public GameObject enemyPrefab;
    public int enemyCount = 3;

    protected override void Start()
    {
        base.Start();
        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 3f;
            Instantiate(enemyPrefab, transform.position + (Vector3)offset, Quaternion.identity);
        }
    }

    void Update()
    {
        if (!isCompleted && Enemy.AliveCount <= 0)
        {
            CompleteRoom();
        }
    }
}
