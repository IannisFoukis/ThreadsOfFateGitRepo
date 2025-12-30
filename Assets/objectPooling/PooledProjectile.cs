using UnityEngine;

public class PooledProjectile : MonoBehaviour
{
    ProjectilePool pool;
    GameObject sourcePrefab;

    public void SetPool(ProjectilePool p)
    {
        pool = p;
    }

    public void SetSourcePrefab(GameObject prefab)
    {
        sourcePrefab = prefab;
    }

    public GameObject SourcePrefab => sourcePrefab;

    public void ReturnToPool()
    {
        if (pool != null)
            pool.Return(this);
        else
            gameObject.SetActive(false);
    }
}
