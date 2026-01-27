using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Biome Config")]
public class BiomeConfig : ScriptableObject
{
    [System.Serializable]
    public class RoomEntry
    {
        [Header("Identity")]
        public RoomRole role;

        [Header("Encounter")]
        public EncounterType encounterType;
        public GameObject enemyPrefab;

        [Header("Shrine")]
        public bool hasShrine;
        public bool shrineDestroyable;
        public ShrineProjectileConfig shrineOverride;

        [Header("Room Contract Flags")]
        public bool silencePhase;
        public bool pressureSpike;
        public bool allowHammer = true;
        public bool allowEncircle = true;
        public bool allowElites = false;
    }


    public RoomEntry[] entries;

    public List<RoomRole> GetRoles()
    {
        var list = new List<RoomRole>();
        if (entries == null) return list;
        foreach (var e in entries)
            list.Add(e.role);
        return list;
    }
}
