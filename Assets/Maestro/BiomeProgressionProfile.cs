using UnityEngine;

[CreateAssetMenu(menuName = "TOF/Biome/Biome Progression Profile")]
public class BiomeProgressionProfile : ScriptableObject
{
    [System.Serializable]
    public struct ChapterRange
    {
        public int chapterIndex;     // 1-based
        public string chapterId;      // semantic label
        public int startRoom;         // inclusive (1-based)
        public int endRoom;           // inclusive
    }

    public ChapterRange[] chapters;

    public bool TryGetChapter(int roomIndex, out ChapterRange chapter)
    {
        foreach (var c in chapters)
        {
            if (roomIndex >= c.startRoom && roomIndex <= c.endRoom)
            {
                chapter = c;
                return true;
            }
        }

        chapter = default;
        return false;
    }
}