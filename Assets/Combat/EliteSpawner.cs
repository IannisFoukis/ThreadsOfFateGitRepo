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

        // Avoid runtime exception if the project doesn't define an "Elite" tag.
        if (IsTagDefined("Elite"))
        {
            elite.tag = "Elite";
        }
        else
        {
            Debug.LogWarning("[ELITE] Tag 'Elite' is not defined in Tag Manager. Using 'Enemy' instead.");
            if (IsTagDefined("Enemy"))
                elite.tag = "Enemy";
        }

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

    static bool IsTagDefined(string tag)
    {
        // Unity throws if tag doesn't exist; use it as a probe.
        try
        {
            GameObject.FindGameObjectWithTag(tag);
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }
}
