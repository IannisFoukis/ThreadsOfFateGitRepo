using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [SerializeField] Projectile projectilePrefab;
    [SerializeField] int initialSize = 5;

    Queue<PooledProjectile> pool = new();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < initialSize; i++)
            CreateOne();
    }

    void CreateOne()
    {
        var proj = Instantiate(projectilePrefab, transform);
        var pooled = proj.GetComponent<PooledProjectile>();

        if (pooled == null)
            pooled = proj.gameObject.AddComponent<PooledProjectile>();

        pooled.SetPool(this);
        proj.gameObject.SetActive(false);
        pool.Enqueue(pooled);
    }

    public Projectile Get()
    {
        if (pool.Count == 0)
            CreateOne();

        var pooled = pool.Dequeue();
        pooled.gameObject.SetActive(true);
        return pooled.GetComponent<Projectile>();
    }

    public void Return(PooledProjectile proj)
    {
        proj.gameObject.SetActive(false);
        pool.Enqueue(proj);
    }
}
