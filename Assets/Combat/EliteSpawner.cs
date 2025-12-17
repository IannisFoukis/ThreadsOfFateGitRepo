using UnityEngine;

public class EliteSpawner : MonoBehaviour
{
    public static EliteSpawner Instance;
    public GameObject eliteEnemyPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnElite()
    {
        Vector3 pos = Vector3.zero;
        GameObject elite = Instantiate(eliteEnemyPrefab, pos, Quaternion.identity);

        if (elite.TryGetComponent<EnemyStatsComponent>(out var stats))
        {
            stats.maxHealth += 5;
            stats.damage += 3;
        }

        Debug.Log("ELITE SPAWNED");
    }
}
