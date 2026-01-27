using UnityEngine;

public class EnemyRoleController : MonoBehaviour
{
    [Header("Behavior Scripts (optional, auto-found if empty)")]
    [SerializeField] private MonoBehaviour melee;
    [SerializeField] private MonoBehaviour ranged;
    [SerializeField] private MonoBehaviour charger;
    [SerializeField] private MonoBehaviour defender;
    [SerializeField] private MonoBehaviour activator;

    [SerializeField] public EnemyRole currentRole = EnemyRole.Melee;

    public EnemyRole CurrentRole => currentRole;

    private void Awake()
    {
        // Auto-find by type name if not wired in inspector (safer in your prefab workflow).
        if (melee == null) melee = GetComponent<EnemyMelee>();
        if (ranged == null) ranged = GetComponent<EnemyRanged>();
        if (charger == null) charger = GetComponent<EnemyCharger>();
        // defender/activator scripts are optional; only wire if you actually have them
        // if (defender == null) defender = GetComponent<EnemyDefender>();
        // if (activator == null) activator = GetComponent<ActivatorRunner>();
        // 🔒 SAFETY: Never allow None to leak
        if (currentRole == EnemyRole.None)
        {
            currentRole = EnemyRole.Melee;
            Debug.Log($"[EnemyRoleController] {name} role corrected to Melee in Awake()");
        }
        //DisableAll();
    }

    public void ApplyRole(EnemyRole newRole)
    {
        currentRole = newRole;

        // NEW: also apply visual color immediately
        ApplyRoleColor();
    }

    private void ApplyRoleColor()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        switch (currentRole)
        {
            case EnemyRole.Melee:
            case EnemyRole.Offender:
               // sr.color = Color.red;
                break;

            case EnemyRole.Elite:

            case EnemyRole.Defender:
                //sr.color = Color.blue;
                break;

            case EnemyRole.Ranged:
            case EnemyRole.Ranger:
               // sr.color = Color.green;
                break;

            case EnemyRole.Activator:
                sr.color = Color.magenta;
                break;

            case EnemyRole.Joker:
                sr.color = Color.yellow;
                break;

            default:
                sr.color = Color.white;
                break;
        }
    }


    private void DisableAll()
    {
        Disable(melee);
        Disable(ranged);
        Disable(charger);
        Disable(defender);
        Disable(activator);
    }

    private static void Enable(MonoBehaviour b) { if (b != null) b.enabled = true; }
    private static void Disable(MonoBehaviour b) { if (b != null) b.enabled = false; }
}
