using UnityEngine;

public class BreatherRoom : RoomController
{
    private bool resolved;

    // Local enum to avoid dependency on a missing/removed global BreatherChoice type.
    public enum BreatherChoice
    {
        Rest,
        Refuse
    }
   

    public void RecordBreatherChoice(BreatherChoice choice)
    {
        if (RunContext.Instance == null)
        {
            UnityEngine.Debug.LogError($"[{nameof(BreatherRoom)}] {nameof(RunContext)}.Instance is null. Cannot record breather choice.");
            return;
        }

        var memory = RunContext.Instance.memory;
        if (memory == null)
        {
            UnityEngine.Debug.LogError($"[{nameof(BreatherRoom)}] RunContext.memory is null. Cannot record breather choice.");
            return;
        }

        switch (choice)
        {
            case BreatherChoice.Rest:
                memory.breatherRestCount++;
                memory.corruption += 1;
                break;

            case BreatherChoice.Refuse:
                memory.breatherRefuseCount++;
                break;
        }
    }

    public void ResolveBreather()
    {
        if (resolved)
        {
            UnityEngine.Debug.Log($"[{nameof(BreatherRoom)}] ResolveBreather called more than once on '{name}'. Ignoring.");
            return;
        }
        resolved = true;

        CompleteRoom(); // triggers RunDirector.OnRoomCompleted()
    }
}
