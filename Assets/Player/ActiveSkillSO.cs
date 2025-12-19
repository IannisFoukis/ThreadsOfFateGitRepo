using UnityEngine;

public abstract class ActiveSkillSO : SkillSO
{
    [Header("Cooldown")]
    public float cooldown = 1f;

    [Header("Charges")]
    public int maxCharges = 1;

    [HideInInspector] public int currentCharges;
    [HideInInspector] public float cooldownTimer;

    public float CooldownNormalized
    {
        get
        {
            if (cooldown <= 0f) return 0f;
            return Mathf.Clamp01(cooldownTimer / cooldown);
        }
    }

    public virtual void OnEquip()
    {
        currentCharges = maxCharges;
        cooldownTimer = 0f;
    }

    public bool CanActivate() => currentCharges > 0 && cooldownTimer <= 0f;

    public void ConsumeCharge()
    {
        currentCharges--;
        cooldownTimer = cooldown;
    }

    public void TickCooldown(float dt)
    {
        if (cooldownTimer > 0f) cooldownTimer -= dt;

        if (cooldownTimer <= 0f && currentCharges < maxCharges)
            currentCharges = maxCharges;
    }

    public abstract void Activate(PlayerController player);
}
