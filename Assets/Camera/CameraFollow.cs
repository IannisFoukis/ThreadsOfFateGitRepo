using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 10f;
    public Vector3 offset;

    void LateUpdate()
    {
        if (!target)
        {
            Debug.LogWarning("CameraFollow: No target assigned");
            return;
        }

        Debug.Log("CameraFollow updating");

        Vector3 desired = target.position + offset;

        CameraClamp clamp = GetComponent<CameraClamp>();
        if (clamp != null)
            desired = clamp.Clamp(desired);

        transform.position = Vector3.Lerp(
            transform.position,
            desired,
            smoothSpeed * Time.deltaTime
        

        );
    }
}
