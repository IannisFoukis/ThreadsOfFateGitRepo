using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Biome Config")]
public class BiomeConfig : ScriptableObject
{
    [System.Serializable]
    public class RoomEntry
    {
        public RoomRole role;
        public EncounterType encounterType;
        public GameObject enemyPrefab;
        public ShrineProjectileConfig shrineOverride;
        // Additional per-room data can be added here (pacing, hazards, etc.)
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
