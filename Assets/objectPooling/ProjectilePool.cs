using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [SerializeField] Projectile projectilePrefab;
    [SerializeField] int initialSize = 5;

    // default pool for the serialized projectilePrefab
    Queue<PooledProjectile> defaultPool = new();

    // pools per prefab (key is the prefab GameObject)
    readonly Dictionary<GameObject, Queue<PooledProjectile>> pools = new();

    void Awake()
    {
        Instance = this;

        if (projectilePrefab != null)
        {
            pools[projectilePrefab.gameObject] = new Queue<PooledProjectile>();
            for (int i = 0; i < initialSize; i++)
                CreateOne(projectilePrefab.gameObject);
        }
    }

    PooledProjectile CreateOne(GameObject prefab)
    {
        var go = Instantiate(prefab, transform);
        var proj = go.GetComponent<Projectile>();
        if (proj == null)
        {
            Debug.LogError("ProjectilePool: prefab does not contain Projectile component");
            Destroy(go);
            return null;
        }

        var pooled = go.GetComponent<PooledProjectile>();
        if (pooled == null)
            pooled = go.gameObject.AddComponent<PooledProjectile>();

        pooled.SetPool(this);
        pooled.SetSourcePrefab(prefab);
        go.gameObject.SetActive(false);

        if (!pools.TryGetValue(prefab, out var q))
        {
            q = new Queue<PooledProjectile>();
            pools[prefab] = q;
        }
        q.Enqueue(pooled);
        return pooled;
    }

    // Get from default pool (uses serialized projectilePrefab)
    public Projectile Get()
    {
        if (projectilePrefab == null) return null;

        var prefab = projectilePrefab.gameObject;
        if (!pools.TryGetValue(prefab, out var q) || q.Count == 0)
        {
            // create more
            CreateOne(prefab);
            q = pools[prefab];
        }

        var pooled = q.Dequeue();
        pooled.gameObject.SetActive(true);
        return pooled.GetComponent<Projectile>();
    }

    // Get pooled projectile for specific prefab
    public Projectile Get(GameObject prefab)
    {
        if (prefab == null) return Get();

        if (!pools.TryGetValue(prefab, out var q) || q.Count == 0)
        {
            // create initial batch
            if (!pools.ContainsKey(prefab))
                pools[prefab] = new Queue<PooledProjectile>();

            for (int i = 0; i < initialSize; i++)
                CreateOne(prefab);

            q = pools[prefab];
        }

        var pooled = q.Dequeue();
        pooled.gameObject.SetActive(true);
        return pooled.GetComponent<Projectile>();
    }

    public void Return(PooledProjectile proj)
    {
        if (proj == null) return;

        proj.gameObject.SetActive(false);

        var prefab = proj.SourcePrefab;
        if (prefab != null && pools.TryGetValue(prefab, out var q))
        {
            q.Enqueue(proj);
            return;
        }

        // fallback: put back into default pool if exists
        if (projectilePrefab != null && pools.TryGetValue(projectilePrefab.gameObject, out var defaultQ))
        {
            defaultQ.Enqueue(proj);
            return;
        }

        // if no pool available, destroy
        Destroy(proj.gameObject);
    }
}
