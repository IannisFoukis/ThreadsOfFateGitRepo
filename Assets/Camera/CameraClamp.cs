using UnityEngine;

public class CameraClamp : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public Vector3 Clamp(Vector3 targetPosition)
    {
        if (CameraBounds.Active == null)
            return targetPosition;

        Bounds b = CameraBounds.Active.bounds;
        Vector3 center = b.center + CameraBounds.Active.transform.position;

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float minX = center.x - b.extents.x + camWidth;
        float maxX = center.x + b.extents.x - camWidth;
        float minY = center.y - b.extents.y + camHeight;
        float maxY = center.y + b.extents.y - camHeight;

        return new Vector3(
            Mathf.Clamp(targetPosition.x, minX, maxX),
            Mathf.Clamp(targetPosition.y, minY, maxY),
            targetPosition.z
        );
    }
}
