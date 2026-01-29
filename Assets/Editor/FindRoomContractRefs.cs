// Assets/Editor/FindRoomContractRefs.cs
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FindRoomContractRefs
{
    [MenuItem("TOF/Diagnostics/Find RoomContract Serialized References")]
    public static void Run()
    {
        // Select RoomContract.cs in Project before running.
        var obj = Selection.activeObject;
        if (obj == null)
        {
            Debug.LogError("Select RoomContract.cs in Project window first.");
            return;
        }

        var path = AssetDatabase.GetAssetPath(obj);
        if (!path.EndsWith(".cs"))
        {
            Debug.LogError("Selection is not a .cs file. Select RoomContract.cs.");
            return;
        }

        var metaPath = path + ".meta";
        if (!File.Exists(metaPath))
        {
            Debug.LogError("Meta file not found: " + metaPath);
            return;
        }

        var meta = File.ReadAllText(metaPath);
        var guidLinePrefix = "guid: ";
        var guidIndex = meta.IndexOf(guidLinePrefix);
        if (guidIndex < 0)
        {
            Debug.LogError("Could not find guid in meta.");
            return;
        }

        var guidStart = guidIndex + guidLinePrefix.Length;
        var guidEnd = meta.IndexOf('\n', guidStart);
        var guid = (guidEnd > guidStart ? meta.Substring(guidStart, guidEnd - guidStart) : meta.Substring(guidStart)).Trim();

        Debug.Log("RoomContract GUID: " + guid);

        string[] exts = { ".unity", ".prefab", ".asset", ".controller", ".mat", ".overrideController" };

        int hits = 0;
        foreach (var assetPath in Directory.EnumerateFiles(Application.dataPath, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(assetPath);
            bool ok = false;
            for (int i = 0; i < exts.Length; i++) if (ext == exts[i]) { ok = true; break; }
            if (!ok) continue;

            // YAML/text assets only; binary assets won't match anyway.
            string text;
            try { text = File.ReadAllText(assetPath); }
            catch { continue; }

            if (text.Contains(guid))
            {
                hits++;
                var unityPath = "Assets" + assetPath.Replace(Application.dataPath, "").Replace('\\', '/');
                Debug.LogWarning("Serialized reference found in: " + unityPath);
            }
        }

        Debug.Log($"Done. Found {hits} file(s) referencing RoomContract GUID.");
    }
}
#endif
