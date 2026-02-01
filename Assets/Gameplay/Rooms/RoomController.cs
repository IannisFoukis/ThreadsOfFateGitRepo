using UnityEngine;

public class RoomController : MonoBehaviour
{
    protected bool roomCompleted;

    protected TacticalAuthority tacticalAuthority;
    protected FormationResolver formationResolver;

    protected virtual void Awake()
    {
        // Cache capabilities
        tacticalAuthority = GetComponent<TacticalAuthority>();
        formationResolver = GetComponent<FormationResolver>();

        // Apply room intent EARLY (before any Start/Update elsewhere)
        var config = GetComponent<RoomConfigController>();
        if (config != null)
            config.Apply();
    }

    protected virtual void Start()
    {
        Debug.Log($"Room started: {gameObject.name}");
    }

    public virtual void CompleteRoom()
    {
        if (roomCompleted)
            return;

        roomCompleted = true;

        Debug.Log($"Room completed: {gameObject.name}");

        var director = Object.FindFirstObjectByType<RoomDirector>();
        if (director == null)
        {
            Debug.LogError("[RoomController] RoomDirector not found");
            return;
        }

        director.NotifyRoomCompleted();
    }
}