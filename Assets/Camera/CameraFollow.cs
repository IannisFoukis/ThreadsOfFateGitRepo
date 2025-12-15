using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 10f;
    public Vector3 offset;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desired,
            smoothSpeed * Time.deltaTime
        );
    }
}
