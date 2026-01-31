using UnityEngine;

public class TacticAuthority : MonoBehaviour
{
    [SerializeField] private TacticalLevel currentLevel;

    public bool CanUse(TacticalLevel required)
    {
        return currentLevel >= required;
    }

    public void SetLevel(TacticalLevel level)
    {
        currentLevel = level;
    }
}