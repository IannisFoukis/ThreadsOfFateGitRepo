using UnityEditor.Rendering.LookDev;
using UnityEngine;

public class RoomDemandTracker : MonoBehaviour
{
    float roomStartTime;
    bool playerHit;
    bool corruptionAcceptedThisRoom;

    void OnEnable()
    {
        roomStartTime = Time.time;
        playerHit = false;
        corruptionAcceptedThisRoom = false;
    }

    // CALLED BY PLAYER HEALTH
    public void OnPlayerHit()
    {
        playerHit = true;
    }

    // CALLED BY CHOICE MANAGER
    public void OnCorruptionAccepted()
    {
        corruptionAcceptedThisRoom = true;
    }

    public void Resolve()
    {
        var demand = GodDirector.Instance.currentDemand;
        bool success = true;

        switch (demand)
        {
            case GodDemand.KillQuickly:
                success = (Time.time - roomStartTime) <= 10f;
                break;

            case GodDemand.TakeNoDamage:
                success = !playerHit;
                break;

            case GodDemand.AcceptCorruption:
                success = corruptionAcceptedThisRoom;
                break;

            case GodDemand.RejectCorruption:
                success = !corruptionAcceptedThisRoom;
                break;

            case GodDemand.DoNothing:
                success = true;
                break;
        }
        if (!success)
        {
            CameraShake.Instance?.Shake(0.15f, 0.1f);
        }

        Debug.Log($"DEMAND {demand} {(success ? "FULFILLED" : "FAILED")}");
        GodDirector.Instance.ResolveDemand(success);
    }
}
