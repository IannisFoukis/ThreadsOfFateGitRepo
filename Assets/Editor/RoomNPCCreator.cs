#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class RoomNPCCreator
{
    const string resourcesDir = "Assets/Resources";
    const string corruptPath = resourcesDir + "/Keeper_IncreaseCorruption.asset";
    const string lockPath = resourcesDir + "/Keeper_LockCorruption.asset";

    static RoomNPCCreator()
    {
        UnityEditor.EditorApplication.delayCall += CreateIfMissing;
    }

    [MenuItem("Tools/Create Keeper RoomNPC Options (Resources)")]
    public static void CreateIfMissing()
    {
        if (!Directory.Exists(resourcesDir))
            Directory.CreateDirectory(resourcesDir);

        if (!File.Exists(corruptPath))
        {
            var a = ScriptableObject.CreateInstance<RoomNPC>();
            a.displayName = "Accept Corruption";
            a.description = "Accept increased corruption for rewards.";
            a.isCursed = true;
            a.effect = RoomNPC.EffectType.Corrupt;
            a.effectValue = 1;
            AssetDatabase.CreateAsset(a, corruptPath);
        }

        if (!File.Exists(lockPath))
        {
            var b = ScriptableObject.CreateInstance<RoomNPC>();
            b.displayName = "Lock Corruption";
            b.description = "Lock corruption level for this run (no further increases).";
            b.isCursed = false;
            b.effect = RoomNPC.EffectType.None; // treat as Lock action
            b.effectValue = 0;
            AssetDatabase.CreateAsset(b, lockPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("RoomNPC keeper options ensured in Resources");
    }
}
#endif
