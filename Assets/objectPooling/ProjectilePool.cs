using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int initialSize = 20;

    private readonly Queue<Projectile> pool = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < initialSize; i++)
        {
            Create();
        }
    }

    private void Create()
    {
        var proj = Instantiate(projectilePrefab, transform);
        proj.AssignPool(this);
        proj.gameObject.SetActive(false);
        pool.Enqueue(proj);
    }

    public Projectile Get()
    {
        if (pool.Count == 0)
            Create();

        return pool.Dequeue();
    }

    public void Return(Projectile projectile)
    {
        pool.Enqueue(projectile);
    }
}
