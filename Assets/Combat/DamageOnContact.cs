using UnityEngine;

public class DamageOnContact : MonoBehaviour
{
    public int damage = 1;
    public Hitbox.OwnerType targetType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Hitbox hitbox = other.GetComponent<Hitbox>();
        if (!hitbox || hitbox.owner != targetType)
            return;

        Health health = other.GetComponentInParent<Health>();
        if (health != null)
        {
            Vector2 dir = (other.transform.position - transform.position).normalized;
            health.TakeDamage(damage, dir);
        }
    }

}
