using TOF.Core.Corruption;
using UnityEngine;
using TMPro;

public class EnemySpeechEmitter : MonoBehaviour
{
    [Header("Speech")]
    public EnemyRole role = EnemyRole.Defender;
    public TextMeshPro textPrefab;

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
        // Phase A discipline:
        // - Defender speaks doctrine / control
        // - Offender speaks ONLY intent
        if (role == EnemyRole.Defender)
        {
            // allowed: existing defender events
        }
        else if (role == EnemyRole.Offender)
        {
            // allowed: offender intent only
            if (evt != EnemySpeechEvent.OffenderHold &&
                evt != EnemySpeechEvent.OffenderCommit &&
                evt != EnemySpeechEvent.OffenderPunish)
                return;
        }
        else
        {
            return;
        }

        string line = GetLine(evt);
        if (string.IsNullOrEmpty(line))
            return;

        ShowText(line);
    }

    private string GetLine(EnemySpeechEvent evt)
    {
        switch (evt)
        {
            case EnemySpeechEvent.DoctrineEngaged: return "Hold the line.";
            case EnemySpeechEvent.Advance: return "Advance.";
            case EnemySpeechEvent.HoldLine: return "Hold.";
            case EnemySpeechEvent.EncircleCall: return "Encircle!";
            case EnemySpeechEvent.FormationBreak: return "Break formation!";
            case EnemySpeechEvent.FanaticLock: return "Stand. Do not move.";
            case EnemySpeechEvent.RetreatCall: return "Fall back!";
            // ───────── Phase A — Offender Lines ─────────
            case EnemySpeechEvent.OffenderHold: return "Hold.";
            case EnemySpeechEvent.OffenderCommit: return "Now.";
            case EnemySpeechEvent.OffenderPunish: return "There.";
        }
        return null;
    }

    private void ShowText(string msg)
    {
        if (textPrefab == null) return;

        var t = Instantiate(
            textPrefab,
            transform.position + Vector3.up * 0.5f + Vector3.back * 1f,
            Quaternion.identity
        );

        t.text = msg;
        t.color = GetColor();

        Destroy(t.gameObject, 1.2f);
    }

    private Color GetColor()
    {
        var room = FindFirstObjectByType<RoomDirector>();
        if (room == null) return Color.white;

        switch (room.corruption)
        {
            case CorruptionTier.Low: return new Color(1f, 1f, 1f, 0.85f);
            case CorruptionTier.Medium: return new Color(1f, 0.8f, 0.6f, 0.9f);
            case CorruptionTier.High: return new Color(1f, 0.4f, 0.4f, 1f);
            case CorruptionTier.Extreme: return new Color(0.8f, 0.1f, 0.1f, 1f);
        }

        return Color.white;
    }
}