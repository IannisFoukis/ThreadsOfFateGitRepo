using UnityEngine;
using System.Collections.Generic;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [SerializeField] PooledProjectile prefab;
    [SerializeField] int preloadCount = 40;

    Queue<PooledProjectile> pool = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < preloadCount; i++)
            Create();
    }

    void Create()
    {
        var p = Instantiate(prefab, transform);
        p.gameObject.SetActive(false);
        pool.Enqueue(p);
    }

    public PooledProjectile Get()
    {
        if (pool.Count == 0)
            Create();

        return pool.Dequeue();
    }

    public void Return(PooledProjectile p)
    {
        pool.Enqueue(p);
    }
}
