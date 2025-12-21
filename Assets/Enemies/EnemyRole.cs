using UnityEngine;

public enum EnemyRole
{
    Melee,
    Ranged,
    Charger,
    Elite
}

public class EnemyRoleController : MonoBehaviour
{
    public EnemyRole role;
}
