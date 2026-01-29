using UnityEngine;

public class RoomMoodDirector : MonoBehaviour
{
    void Start()
    {
        var contract = RoomAccess.Current;
        if (contract == null)
            return;

        Debug.Log($"[Mood] Room mood start | Silence={contract.silencePhase} Pressure={contract.pressureSpike}");

        if (contract.silencePhase)
            ApplySilenceMood();

        if (contract.pressureSpike)
            ApplyPressureMood();

        if (contract.reduceAudio)
            ApplyReducedAudioMood();

        if (contract.hasShrine)
            ApplyShrineMood();
    }

    void ApplySilenceMood()
    {
        Debug.Log("[Mood] Silence mood active");
        // later: low-pass filter, reverb, desat, vignette
    }

    void ApplyPressureMood()
    {
        Debug.Log("[Mood] Pressure mood active");
        // later: camera shake, pulse, chromatic shift
    }

    void ApplyReducedAudioMood()
    {
        Debug.Log("[Mood] Reduced audio mood active");
        // later: master volume down
    }

    void ApplyShrineMood()
    {
        Debug.Log("[Mood] Shrine mood active");
        // later: hum, glow, particles
    }
}
