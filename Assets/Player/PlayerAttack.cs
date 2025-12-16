using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Collider2D attackCollider;
    public float attackDuration = 0.2f;
    public float attackCooldown = 0.4f;

    bool canAttack = true;
    Vector2 lastDirection = Vector2.right;

    void Update()
    {
        if (GameLock.IsLocked) return;

        ReadDirection();
        HandleAttackInput();
    }

    void ReadDirection()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 dir = new Vector2(x, y);
        if (dir.sqrMagnitude > 0.01f)
            lastDirection = dir.normalized;
    }

    void HandleAttackInput()
    {
        if (!canAttack) return;

        if (Input.GetKeyDown(KeyCode.J)) // TEMP attack key
        {
            StartCoroutine(DoAttack());
        }
    }

    System.Collections.IEnumerator DoAttack()
    {
        canAttack = false;

        PositionAttackHitbox();

        attackCollider.enabled = true;
        yield return new WaitForSeconds(attackDuration);
        attackCollider.enabled = false;

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void PositionAttackHitbox()
    {
        attackCollider.transform.localPosition = lastDirection;
    }
}
