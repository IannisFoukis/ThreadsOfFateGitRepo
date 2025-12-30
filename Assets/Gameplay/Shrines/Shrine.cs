using UnityEngine;

public class Shrine : MonoBehaviour
{
    public ShrineType type;
    public int corruptionThreshold = 2;
    public ShrineTier currentTier;
    [SerializeField] ShrineAttackController attackController;
    public bool IsActive { get; private set; }

    void Start()
    {
        IsActive = false;
    }

    // Prepare shrine for room entry without starting hazards/attacks.
    // This updates the shrine tier and visuals so other systems (eg. room scaling)
    // can query the correct tier before the shrine is actually activated by an activator.
    public void PrepareForRoom()
    {
        // Do not change IsActive; only determine tier and visuals
        DetermineTier();
        ApplyTierVisuals();
    }

    public enum ShrineTier
    {
        Tier1,
        Tier2,
        Tier3
    }

    public float GetEnemyHpMultiplier()
    {
        return currentTier switch
        {
            ShrineTier.Tier1 => 1.2f,
            ShrineTier.Tier2 => 1.2f,
            ShrineTier.Tier3 => 1.4f,
            _ => 1f
        };
    }

    public float GetEnemyDamageMultiplier()
    {
        return currentTier switch
        {
            ShrineTier.Tier1 => 1f,
            ShrineTier.Tier2 => 1.2f,
            ShrineTier.Tier3 => 1.3f,
            _ => 1f
        };
    }

    // Activator-based activation path – no pure/corrupted effect, just hazards
    public void ActivateByActivator()
    {
        if (IsActive) return;

        IsActive = true;

        // Make sure tier is up to date
        DetermineTier();
        ApplyTierVisuals();

        if (attackController == null)
            attackController = GetComponent<ShrineAttackController>();

        if (attackController != null)
        {
            attackController.StartAttacksForTier(currentTier);
        }
    }

    public void Activate()
    {
        if (IsActive) return;

        // BUG FIX: this must be true
        IsActive = true;

        int corruption = RunCorruptionState.Instance.CorruptionLevel;

        Debug.Log($"Shrine {type} activated at corruption {corruption}");

        // First determine correct tier and visuals
        DetermineTier();
        ApplyTierVisuals();

        // Start bullet hell / hazards
        if (attackController == null)
            attackController = GetComponent<ShrineAttackController>();

        if (attackController != null)
        {
            attackController.StartAttacksForTier(currentTier);
        }

        // Then apply pure / corrupted effect
        if (corruption >= corruptionThreshold)
            ApplyCorruptedEffect();
        else
            ApplyPureEffect();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var role = other.GetComponent<EnemyRoleController>();
        if (role != null && role.role == EnemyRole.Activator)
        {
            // BUG FIX: use the dedicated activator path
            ActivateByActivator();
        }
    }

    void DetermineTier()
    {
        int corruption = FindAnyObjectByType<GameStateManager>().RunData.corruption;

        if (corruption >= 6)
            currentTier = ShrineTier.Tier3;
        else if (corruption >= 3)
            currentTier = ShrineTier.Tier2;
        else
            currentTier = ShrineTier.Tier1;

        Debug.Log($"[SHRINE] {name} activated at {currentTier}");
    }

    void ApplyTierVisuals()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null) return;

        sr.color = currentTier switch
        {
            ShrineTier.Tier1 => Color.white,
            ShrineTier.Tier2 => new Color(1f, 0.8f, 0.5f),
            ShrineTier.Tier3 => new Color(1f, 0.4f, 0.4f),
            _ => Color.white
        };
    }

    void ApplyPureEffect()
    {
        Debug.Log($"Shrine {type}: PURE EFFECT");

        switch (type)
        {
            case ShrineType.Blood:
                PlayerStats.Instance.Heal(1);
                break;

            case ShrineType.Order:
                Debug.Log("Enemies spawn slower");
                break;

            case ShrineType.Silence:
                Debug.Log("No special effect");
                break;
        }
    }

    void ApplyCorruptedEffect()
    {
        Debug.Log($"Shrine {type}: CORRUPTED EFFECT");

        switch (type)
        {
            case ShrineType.Blood:
                CombatModifiers.GlobalEnemyLifesteal += 1;
                break;

            case ShrineType.Chaos:
                CombatModifiers.RandomizeEnemyRoles = true;
                break;
        }
    }

    void ApplyBloodEffect()
    {
        switch (currentTier)
        {
            case ShrineTier.Tier1:
                PlayerStats.Instance.Heal(1);
                break;

            case ShrineTier.Tier2:
                PlayerStats.Instance.Heal(2);
                RunCorruptionState.Instance.AcceptCorruption();
                break;

            case ShrineTier.Tier3:
                PlayerStats.Instance.Heal(3);
                RunCorruptionState.Instance.AcceptCorruption();
                RunCorruptionState.Instance.AcceptCorruption(); // double hit
                break;
        }

        Debug.Log($"[SHRINE] Blood shrine applied {currentTier}");
    }

    public void ForceHazards()
    {
        // Only apply Blood-specific effect if shrine is actually Blood
        if (type == ShrineType.Blood)
        {
            ApplyBloodEffect();
        }

        ApplyTierVisuals();
        //ApplyHazards();
        Debug.Log("[SHRINE] Hazards forced by corruption");
    }
}
