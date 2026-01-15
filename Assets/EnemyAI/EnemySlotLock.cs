using UnityEngine;

public class EnemySlotLock : MonoBehaviour
{
    public bool HasSlot => hasSlot;
    public TacticSlotType CurrentSlot => currentSlot;
    public bool CanAcceptSlot => !hasSlot && cooldownTimer <= 0f;

    [SerializeField] float breakCooldown = 1.5f;

    bool hasSlot;
    TacticSlotType currentSlot;
    Vector2 slotWorldPos;
    float cooldownTimer;

    public void LockSlot(TacticSlotType slot, Vector2 worldPos)
    {
        hasSlot = true;
        currentSlot = slot;
        slotWorldPos = worldPos;

        Debug.Log($"[SlotLock] {name} LOCKED {slot}");
    }

    public void UpdateSlotPosition(Vector2 worldPos)
    {
        if (!hasSlot) return;
        slotWorldPos = worldPos;
    }

    public Vector2 GetSlotPosition()
    {
        return slotWorldPos;
    }

    public void ReleaseSlot(float breakForce = 0f)
    {
        if (!hasSlot) return;

        Debug.Log($"[SlotLock] {name} RELEASED {currentSlot}");

        hasSlot = false;
        cooldownTimer = breakCooldown;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    public bool CanTakeSlot(TacticSlotType slot) => CanAcceptSlot;
}
