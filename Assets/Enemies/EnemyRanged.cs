using UnityEngine;

public class EnemyRanged : MonoBehaviour
{
    [SerializeField] float fireCooldown = 1.6f;
    float cooldown;

    EnemyChase chase;

    void Awake()
    {
        chase = GetComponent<EnemyChase>();
    }

    void Update()
    {
        if (cooldown > 0)
            cooldown -= Time.deltaTime;
    }

    public void Fire()
    {
        if (cooldown > 0) return;
        cooldown = fireCooldown;
        // projectile handled elsewhere
    }

    public void MultiplyFireRate(float multiplier)
    {
        fireCooldown /= multiplier;
    }

    public void SetSpeedMultiplier(float value)
    {
        chase?.SetSpeedMultiplier(value);
    }
}
