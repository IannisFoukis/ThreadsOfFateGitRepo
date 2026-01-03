using UnityEngine;

public class BreatherEffectApplier : BreatherInteraction
{
    public RoomNPC npcOption;

    protected override void Execute()
    {
        Debug.Log($"[BreatherEffectApplier] Executing NPC option: {npcOption.displayName}");
        Debug.Log($"  isCursed = {npcOption.isCursed}");
        Debug.Log($"  effect = {npcOption.effect}");
        Debug.Log($"  value = {npcOption.effectValue}");

        

        if (npcOption == null) return;


        if (npcOption.isCursed)
        {
            Debug.Log("[BreatherEffectApplier] Corruption ACCEPTED");
            FindAnyObjectByType<RoomDemandTracker>()?.OnCorruptionAccepted();
        }
        else
        {
            Debug.Log("[BreatherEffectApplier] Corruption REJECTED");
        }


        // Notify demand tracker (optional)
        if (npcOption.isCursed)
            FindAnyObjectByType<RoomDemandTracker>()?.OnCorruptionAccepted();

        // Apply to run
        var director = FindFirstObjectByType<RunDirector>();
        if (director == null)
        {
            Debug.LogError("[BreatherEffectApplier] No RunDirector found in scene.");
            return;
        }

        // call whatever method you already have / will add
        director.ApplyRoomNPC(npcOption);
    }
    protected override void PlayVisualFeedback()
    {
        if (npcOption == null) return;

        if (npcOption.isCursed)
        {
            Debug.Log("[BreatherVisual] Accept Corruption feedback");

            CameraShake.Instance?.Shake(0.25f, 0.2f);

            BreatherVisualPulse pulse =
                FindFirstObjectByType<BreatherVisualPulse>();
            pulse?.Pulse(Color.red);
        }
        else
        {
            Debug.Log("[BreatherVisual] Reject Corruption feedback");

            CameraShake.Instance?.Shake(0.15f, 0.1f);

            BreatherVisualPulse pulse =
                FindFirstObjectByType<BreatherVisualPulse>();
            pulse?.Pulse(Color.white);
        }
    }

}
