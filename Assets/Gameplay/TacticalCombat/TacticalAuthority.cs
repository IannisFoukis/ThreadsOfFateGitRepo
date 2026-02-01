using UnityEngine;

public class TacticalAuthority : MonoBehaviour
{
    [SerializeField] private TacticalLevel currentLevel = TacticalLevel.Instinct;

    public TacticalLevel CurrentLevel => currentLevel;

    public bool Allows(TacticalLevel required)
    {
        return currentLevel >= required;
    }

    public void SetLevel(TacticalLevel level)
    {
        currentLevel = level;
    }
}