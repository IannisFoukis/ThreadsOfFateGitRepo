using UnityEngine;

public enum EnemyRole
{
    Offender,
    Defender,
    Support,
    Activator,
    Flanker,
    Melee,
    Ranged,
    Charger,
    Elite
}

public class EnemyRoleController : MonoBehaviour
{
    [Header("Assigned at spawn")]
    public EnemyRole role;
}
