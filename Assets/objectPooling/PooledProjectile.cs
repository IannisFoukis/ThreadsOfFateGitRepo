using UnityEngine;

public class PooledProjectile : MonoBehaviour
{
    ProjectilePool pool;

    public void SetPool(ProjectilePool p)
    {
        pool = p;
    }

    public void ReturnToPool()
    {
        if (pool != null)
            pool.Return(this);
        else
            gameObject.SetActive(false);
    }
}
