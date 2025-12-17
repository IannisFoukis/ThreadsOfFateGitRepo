using UnityEngine;

public class Shrine : MonoBehaviour
{
    public ShrineType type;
    public int corruptionThreshold = 2;

    public void Activate()
    {
        int corruption = RunCorruptionState.Instance.CorruptionLevel;

        Debug.Log($"Shrine {type} activated at corruption {corruption}");

        if (corruption >= corruptionThreshold)
            ApplyCorruptedEffect();
        else
            ApplyPureEffect();
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
}
