using UnityEngine;

public static class RoomAccess
{
    public static RoomContract Current
    {
        get
        {
            var rd = Object.FindFirstObjectByType<RoomDirector>();
            return rd != null ? rd.contract : null;
        }
    }
}
