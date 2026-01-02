using UnityEngine;

// Risk room: higher difficulty, offers stronger rewards via a contract
public class RiskRoom : RoomController
{
    [SerializeField] bool doubleShrine = true;
    [SerializeField] int extraTensionOnEnter = 2;

    GameStateManager gsm;

    protected override void Start()
    {
        base.Start();
        gsm = FindAnyObjectByType<GameStateManager>();
        if (gsm == null)
        {
            Debug.LogError("RiskRoom: GameStateManager not found");
            Invoke(nameof(Finish), 1f);
            return;
        }

        // Apply contract: increase room tension and optionally double shrine tier effects
        gsm.RunState.runTension += extraTensionOnEnter;
        Debug.Log("RiskRoom: applied extra tension");

        if (doubleShrine)
        {
            // Use corruption as a simple proxy to force higher shrine tiers for this room
            gsm.RunData.corruption += 2;
            Debug.Log("RiskRoom: increased run corruption proxy for shrine scaling");
        }

        Invoke(nameof(Finish), 3f);
    }

    void Finish()
    {
        CompleteRoom();
    }
}
