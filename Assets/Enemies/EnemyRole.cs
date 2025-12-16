using UnityEngine;

public enum EnemyRole
{
    Melee,
    Ranged,
    Charger
}

public class EnemyRoleController : MonoBehaviour
{
    public EnemyRole role;
}
