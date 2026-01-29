using UnityEngine;

[CreateAssetMenu(menuName = "TOF/Room Contract")]
public class RoomContract : ScriptableObject
{
    [Header("Identity")]
    public string contractName;
    public RoomRole roomRole;

    [Header("Combat Toggles")]
    public bool enableCombat = true;
    public bool useCoordinator = true;

    [Header("Enemy Composition (G1)")]
    public int offenders = 3;
    public int defenders = 2;
    public int rangers = 1;
    public int activators = 0;
    public int jokers = 0;

    [Range(0f, 1f)]
    public float eliteChance = 0.15f;

    [Header("Formation Permissions")]
    public bool allowPhalanx = true;
    public bool allowFlank = true;
    public bool allowSwarm = false;

    [Header("Shrine")]
    public bool hasShrine = false;
    public bool shrineDestroyable = false;

    [Header("Silence Phase")]
    public bool silencePhase = false;

    [Header("Pressure")]
    public bool pressureSpike = false;

    [Header("Encounter Rules")]
    public bool allowHammer = false;
    public bool allowEncircle = false;
    public float hammerAggression = 1f;
    public float encircleSpeedMultiplier = 1f;

    [Header("Variants")]
    [Range(0f, 1f)] public float fakeOutChance = 0f;
    [Range(0f, 1f)] public float delayedDashChance = 0f;
    public bool allowElites = false;

    [Header("Audio / Atmosphere")]
    public bool reduceAudio = false;
}
