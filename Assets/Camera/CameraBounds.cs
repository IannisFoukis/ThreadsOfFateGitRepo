using UnityEngine;

public class CameraBounds : MonoBehaviour
{
    public static CameraBounds Active;

    public Bounds bounds;

    private void OnEnable()
    {
        Active = this;
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(bounds.center + transform.position, bounds.size);
    }
}
