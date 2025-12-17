using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    public int maxHealth = 3;
    public int damage = 1;
    public float moveSpeed = 2f;
}
