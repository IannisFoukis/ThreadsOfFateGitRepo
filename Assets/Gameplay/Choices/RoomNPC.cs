using UnityEngine;

[CreateAssetMenu(menuName = "Game/Room NPC")]
public class RoomNPC : ScriptableObject
{
    public string displayName;
    public Sprite portrait;
    [TextArea] public string description;
    public bool isCursed;

    public enum EffectType
    {
        None,
        Heal,
        Corrupt,
        SpawnElite,
        GrantItem,
        EnvironmentalInstability
    }

    public EffectType effect;
    public int effectValue = 1;
}
