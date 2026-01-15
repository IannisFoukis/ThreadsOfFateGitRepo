using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    [Header("Aggro")]
    public float aggroRange = 30f;

    [Header("Tactical Slots")]
    public float slotHoldDuration = 4.5f;
    public float slotLockDuration = 1.2f;

    public bool HasSlot { get; private set; }
    public bool IsSlotLocked => HasSlot && slotLockTimer > 0f;

    public TacticSlotType CurrentSlot { get; private set; } = TacticSlotType.None;
    public Vector2 SlotPosition { get; private set; }

    private float slotHoldTimer;
    private float slotLockTimer;

    void Update()
    {
        UpdateSlotTimers();
    }

    private void UpdateSlotTimers()
    {
        if (!HasSlot) return;

        if (slotLockTimer > 0f)
            slotLockTimer -= Time.deltaTime;

        if (slotHoldTimer > 0f)
            slotHoldTimer -= Time.deltaTime;

        if (slotHoldTimer <= 0f)
            ExitSlot();
    }

    public void EnterSlot(TacticSlotType slot, Vector2 position)
    {
        HasSlot = true;
        CurrentSlot = slot;
        SlotPosition = position;

        slotHoldTimer = slotHoldDuration;
        slotLockTimer = slotLockDuration;

        Debug.Log($"[ENEMY] {name} locked slot {slot} at {position}");
    }

    public void ExitSlot()
    {
        if (!HasSlot) return;

        Debug.Log($"[ENEMY] {name} released slot {CurrentSlot}");

        HasSlot = false;
        CurrentSlot = TacticSlotType.None;
        slotHoldTimer = 0f;
        slotLockTimer = 0f;
    }

    private void OnEnable()
    {
        var director = FindAnyObjectByType<TacticDirector>();
        if (director != null)
            director.Register(this);
    }

    private void OnDisable()
    {
        var director = FindAnyObjectByType<TacticDirector>();
        if (director != null)
            director.Unregister(this);
    }

    private void OnDrawGizmos()
    {
        if (!HasSlot) return;

        Gizmos.color = IsSlotLocked ? Color.red : Color.yellow;
        Gizmos.DrawSphere(SlotPosition, 0.25f);
        Gizmos.DrawLine(transform.position, SlotPosition);
    }
}
