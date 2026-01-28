using UnityEngine;


[CreateAssetMenu(menuName = "TOF/Rooms/Room Contract")]
public class RoomContractSO : ScriptableObject
{
    [Header("Identity")]
    public RoomRole roomRole;
    public string roomId;

    [Header("Flow Rules")]
    public bool allowsCombat = false;
    public bool allowsShrine = false;
    public bool autoComplete = false;

    [Header("Combat Rules")]
    public EncounterDoctrineSO encounterDoctrine;
    public int baseTension = 0;

    [Header("Meta Influence")]
    public int corruptionWeight = 0;
    public bool affectsKeeperJudgment = false;
}
