using UnityEngine;
using static UnityEditor.PlayerSettings;

public class EnemySlotLock : MonoBehaviour
{

    /// <summary>
    /// ///API///////////////////////
    /// </summary>
    public bool HasSlot { get; private set; }
    public Vector2 SlotPosition { get; private set; }

    public TacticSlotType CurrentSlot { get; private set; }

    private Vector2 lockedPosition;





    /// <summary>
    /// ///////////LOGIC////////////////////
    /// </summary>
    /// <returns></returns>
    public Vector2 GetLockedPosition()
    {
        return lockedPosition;
    }

    public bool CanAcceptSlot => !HasSlot;

    public bool CanTakeSlot(TacticSlotType slot)
    {
        return !HasSlot;
    }

    public void LockSlot(TacticSlotType slot, Vector2 position)
    {
        HasSlot = true;
        SlotPosition = position;

        CurrentSlot = slot;
        lockedPosition = position;

        Debug.Log($"[SlotLock] {name} LOCKED {slot}");
    }

    public void UpdateSlotPosition(Vector2 position)
    {
        if (!HasSlot) return;
        lockedPosition = position;
    }

    public void ReleaseSlot(float breakForce = 0f)
    {
        if (!HasSlot) return;

        Debug.Log($"[SlotLock] {name} RELEASED {CurrentSlot}");

        HasSlot = false;
        CurrentSlot = TacticSlotType.None;
    }
}
