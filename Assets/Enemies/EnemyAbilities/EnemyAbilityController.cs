using UnityEngine;
using static Shrine;

public class EnemyAbilityController : MonoBehaviour
{
    public EnemyAbilityTier currentTier = EnemyAbilityTier.Base;

    // Cached optional abilities
    EnemyDash dash;
   // EnemyShield shield;
   // EnemyTeleport teleport;
  // EnemySummon summon;

    void Awake()
    {
        dash = GetComponent<EnemyDash>();
       // shield = GetComponent<EnemyShield>();
        //teleport = GetComponent<EnemyTeleport>();
        //summon = GetComponent<EnemySummon>();

        DisableAll();
    }

    void DisableAll()
    {
        dash?.gameObject.SetActive(false);
       // shield?.gameObject.SetActive(false);
       // teleport?.gameObject.SetActive(false);
       // summon?.gameObject.SetActive(false);
    }

    public void ApplyEscalation(
    EnemyRole role,
    ShrineTier shrineTier,
    int runTension,
    bool midFight)
    {
        DisableAll();

        switch (role)
        {
            case EnemyRole.Melee:
                if (shrineTier >= ShrineTier.Tier2 || runTension >= 5)
                    dash?.gameObject.SetActive(true);

               // if (shrineTier == ShrineTier.Tier3 || midFight)
                   // shield?.gameObject.SetActive(true);
                break;

            case EnemyRole.Ranged:
               // if (shrineTier >= ShrineTier.Tier2 || runTension >= 4)
                  // burstFire?.gameObject.SetActive(true);

                //if (shrineTier == ShrineTier.Tier3 || midFight)
                 //  teleport?.gameObject.SetActive(true);
                break;

            case EnemyRole.Charger:
                if (shrineTier >= ShrineTier.Tier2 || runTension >= 6)
                    dash?.gameObject.SetActive(true);

                //if (shrineTier == ShrineTier.Tier3 || midFight)
                   // summon?.gameObject.SetActive(true);
                break;
        }

        Debug.Log(
     $"[ABILITY] {name} | Role={role} | Shrine={shrineTier} | Tension={runTension} | MidFight={midFight}"
 );

    }
    public void FireProjectile(Vector2 dir)
    {
        var p = ProjectilePool.Instance.Get();
        p.transform.position = transform.position;
        p.Fire(dir, 8f, 3f);
    }

}
