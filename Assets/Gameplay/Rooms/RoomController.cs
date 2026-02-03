using UnityEngine;

public class RoomController : MonoBehaviour
{
    protected bool roomCompleted;
    protected RoomContext roomContext;
    protected TacticalAuthority tacticalAuthority;
    protected FormationResolver formationResolver;

    protected virtual void Awake()
    {
        tacticalAuthority = GetComponent<TacticalAuthority>();
        formationResolver = GetComponent<FormationResolver>();

        // 🔒 Initialize room identity context (SAFE DEFAULTS)
        roomContext = new RoomContext
        {
            roomIndex = 0,
            chapterIndex = 0,
            chapterId = "",
            roomRole = RoomRole.Combat
        };

        // Apply room intent EARLY
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