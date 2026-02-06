using UnityEngine;
using System;

public class FormationResolver : MonoBehaviour
{
    public FormationType Current { get; private set; } = FormationType.Swarm;

    /// <summary>
    /// Fired when formation changes.
    /// EncounterCoordinator is the ONLY intended listener in Phase C3.
    /// </summary>
    public event Action<FormationType> OnFormationChanged;

    public void SetFormation(FormationType formation)
    {
        if (Current == formation)
            return;

        Current = formation;
        OnFormationChanged?.Invoke(Current);

        Debug.Log($"[FormationResolver] Formation set → {Current}");
    }
}
