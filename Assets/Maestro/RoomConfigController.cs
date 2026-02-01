using UnityEngine;

public class RoomConfigController : MonoBehaviour
{
    [Header("Tactics")]
    public TacticalLevel tacticalLevel = TacticalLevel.Instinct;

    [Header("Formation")]
    public FormationType initialFormation = FormationType.Swarm;

    [Header("Debug")]
    public bool verbose = false;

    public void Apply()
    {
        var tacticalAuthority = GetComponent<TacticalAuthority>();
        if (tacticalAuthority != null)
        {
            tacticalAuthority.SetLevel(tacticalLevel);
            if (verbose)
                Debug.Log($"[RoomConfig] TacticalLevel = {tacticalLevel}");
        }

        var formationResolver = GetComponent<FormationResolver>();
        if (formationResolver != null)
        {
            formationResolver.SetFormation(initialFormation);
            if (verbose)
                Debug.Log($"[RoomConfig] Formation = {initialFormation}");
        }
    }
}