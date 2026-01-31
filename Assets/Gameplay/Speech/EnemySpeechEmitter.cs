using TOF.Core.Corruption;
using UnityEngine;

public class EnemySpeechEmitter : MonoBehaviour
{
    [Header("Speech")]
    public EnemyRole role;
    public TMPro.TextMeshPro textPrefab;

    private void OnEnable()
    {
        SpeechBus.OnSpeechEvent += HandleSpeech;
    }

    private void OnDisable()
    {
        SpeechBus.OnSpeechEvent -= HandleSpeech;
    }

    private void HandleSpeech(EnemySpeechEvent evt)
    {
        if (role != EnemyRole.Defender)
            return;

        string line = GetLine(evt);
        if (string.IsNullOrEmpty(line))
            return;

        ShowText(line);
    }

    private string GetLine(EnemySpeechEvent evt)
    {
        switch (evt)
        {
            case EnemySpeechEvent.DoctrineEngaged:
                return "Hold the line.";
            case EnemySpeechEvent.FormationBreak:
                return "Break formation!";
            case EnemySpeechEvent.FanaticLock:
                return "Stand. Do not move.";
            case EnemySpeechEvent.SacrificeIntent:
                return "I will hold them!";

        }
        return null;
    }

    private void ShowText(string msg)
    {
        Debug.Log("[Speech] Showing text");

        var text = Instantiate(textPrefab, transform.position + Vector3.up * 0.5f + Vector3.back * 1f,
    Quaternion.identity
                            );


    }
    private Color GetColor()
    {
        var room = FindFirstObjectByType<RoomDirector>();
        if (room == null) return Color.white;

        switch (room.corruption)
        {
            case CorruptionTier.Low:
                return new Color(1f, 1f, 1f, 0.85f); // pale
            case CorruptionTier.Medium:
                return new Color(1f, 0.8f, 0.6f, 0.9f);
            case CorruptionTier.High:
                return new Color(1f, 0.4f, 0.4f, 1f);
            case CorruptionTier.Extreme:
                return new Color(0.8f, 0.1f, 0.1f, 1f);
        }

        return Color.white;
    }

}
