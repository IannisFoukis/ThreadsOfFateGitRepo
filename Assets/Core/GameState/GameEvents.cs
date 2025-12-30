using System;

// Central static event hub for cross-system orchestration
public static class GameEvents
{
    // Room lifecycle
    public static event Action OnRoomStart;
    public static event Action OnRoomCompleted;

    // Run lifecycle
    public static event Action OnRunStart;

    // Enemy lifecycle
    public static event Action<UnityEngine.GameObject> OnEnemySpawned;

    // Mid-fight escalation
    public static event Action OnMidFightEscalation;

    // Shrine
    public static event Action<UnityEngine.GameObject> OnShrineActivated;

    // Corruption
    public static event Action<int> OnCorruptionChanged;

    // Helper invokers (optional)
    public static void RaiseRoomStart() => OnRoomStart?.Invoke();
    public static void RaiseRoomCompleted() => OnRoomCompleted?.Invoke();
    public static void RaiseRunStart() => OnRunStart?.Invoke();
    public static void RaiseEnemySpawned(UnityEngine.GameObject enemy) => OnEnemySpawned?.Invoke(enemy);
    public static void RaiseMidFightEscalation() => OnMidFightEscalation?.Invoke();
    public static void RaiseShrineActivated(UnityEngine.GameObject shrine) => OnShrineActivated?.Invoke(shrine);
    public static void RaiseCorruptionChanged(int level) => OnCorruptionChanged?.Invoke(level);
}
