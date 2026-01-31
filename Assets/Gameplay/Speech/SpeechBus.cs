using System;
using UnityEngine;

public static class SpeechBus
{
    public static event Action<EnemySpeechEvent> OnSpeechEvent;

    private static float lastTime;
    private const float GLOBAL_COOLDOWN = 1.0f;

    public static void Emit(EnemySpeechEvent evt)
    {
        if (Time.time - lastTime < GLOBAL_COOLDOWN)
            return;

        lastTime = Time.time;
        OnSpeechEvent?.Invoke(evt);
    }
}
