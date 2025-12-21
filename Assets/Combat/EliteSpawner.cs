using UnityEngine;

public class EliteSpawner : MonoBehaviour
{
    public static EliteSpawner Instance;
    public GameObject eliteEnemyPrefab;
    public bool EliteAlive { get; private set; }
    void Awake()
    {
        Instance = this;
    }

    public void SpawnElite()
    {
        EliteAlive = true;
        Vector3 pos = Vector3.zero;
        GameObject elite = Instantiate(eliteEnemyPrefab, pos, Quaternion.identity);

        if (elite.TryGetComponent<EnemyStatsComponent>(out var stats))
        {
            stats.maxHealth += 5;
            stats.damage += 3;
        }

        Debug.Log("ELITE SPAWNED");
    }
    public void OnEliteKilled()
    {
        EliteAlive = false;
    }

}
