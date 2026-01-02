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
        if (EliteAlive) return;

        EliteAlive = true;

        GameObject elite = Instantiate(
            eliteEnemyPrefab,
            Vector3.zero,
            Quaternion.identity
        );

        elite.tag = "Elite";

        if (elite.TryGetComponent<EnemyStatsComponent>(out var stats))
        {
            stats.maxHealth += 5;
            stats.damage += 3;
        }

        GameEvents.RaiseEnemySpawned(elite);
        Debug.Log("[ELITE] Spawned");
    }

    public void OnEliteKilled()
    {
        if (!EliteAlive) return;

        EliteAlive = false;
        Debug.Log("[ELITE] Killed");
    }
}
