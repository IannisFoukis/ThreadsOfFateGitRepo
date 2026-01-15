using UnityEngine;

public class EnemyMelee : MonoBehaviour
{
    [SerializeField] float attackCooldown = 1.2f;
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

    public void OnAttack()
    {
        if (cooldown > 0) return;
        cooldown = attackCooldown;
    }

    public void SetSpeedMultiplier(float value)
    {
        chase?.SetSpeedMultiplier(value);
    }
}
