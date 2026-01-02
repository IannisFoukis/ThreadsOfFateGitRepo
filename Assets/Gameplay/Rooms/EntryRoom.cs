using UnityEngine;
using System.Collections.Generic;

public class EntryRoom : RoomController
{
    protected override void Start()
    {
        base.Start();
        // Show keeper UI (choices) then finish after player chooses or timeout
        ShowKeeperChoices();
    }

    void Finish()
    {
        CompleteRoom();
    }

    void ShowKeeperChoices()
    {
        // Load keeper options from Resources
        var optA = Resources.Load<RoomNPC>("Keeper_IncreaseCorruption");
        var optB = Resources.Load<RoomNPC>("Keeper_LockCorruption");

        var options = new System.Collections.Generic.List<RoomNPC>();
        if (optA != null) options.Add(optA);
        if (optB != null) options.Add(optB);

        if (options.Count == 0)
        {
            // fallback: finish quickly
            Invoke(nameof(Finish), 2f);
            return;
        }

        // Show UI
        var ui = FindAnyObjectByType<NonCombatUIController>();
        if (ui != null)
        {
            ui.Show(options.ToArray(), OnKeeperChoice, "The Keeper Offers", null);
        }
        else
        {
            Debug.LogWarning("No NonCombatUIController found");
            Invoke(nameof(Finish), 2f);
        }
    }

    void OnKeeperChoice(int index)
    {
        Debug.Log($"EntryRoom: OnKeeperChoice called with index={index}");
        // Apply selected effect
        RoomNPC chosen = null;
        if (index == 0)
            chosen = Resources.Load<RoomNPC>("Keeper_IncreaseCorruption");
        else if (index == 1)
            chosen = Resources.Load<RoomNPC>("Keeper_LockCorruption");

        if (chosen != null)
        {
            switch (chosen.effect)
            {
                case RoomNPC.EffectType.Corrupt:
                    RunCorruptionState.Instance?.Increase(chosen.effectValue);
                    break;
                case RoomNPC.EffectType.None:
                    RunCorruptionState.Instance?.LockCorruption();
                    break;
            }
        }

        // finish room after short delay
        // Ensure any choice locks are cleared so player regains control
        ChoiceManager.Instance?.FinishChoice();

        Invoke(nameof(Finish), 0.5f);
    }
}
