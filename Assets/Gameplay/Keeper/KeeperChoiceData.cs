using UnityEngine;

[CreateAssetMenu(menuName = "Keeper/Choice")]
public class KeeperChoiceData : ScriptableObject
{
    [TextArea(2, 4)]
    public string keeperLine;

    public string optionAText;
    public string optionBText;
    public string optionCText;

    public KeeperChoice optionA;
    public KeeperChoice optionB;
    public KeeperChoice optionC;
}
