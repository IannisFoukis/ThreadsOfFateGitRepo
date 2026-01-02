using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 10f;
    public Vector3 offset;
    // Prevent spamming the console when no target is present
    // Scene subscription to pick up player when scenes load
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Try to acquire at start if player already exists in scene
        var p = GameObject.FindWithTag("Player");
        if (p != null)
            target = p.transform;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // When a new scene loads, try to find the player and assign the camera target
        var p = GameObject.FindWithTag("Player");
        if (p != null)
            target = p.transform;
    }

    void LateUpdate()
    {
        if (!target)
            return; // nothing to do until a scene load assigns the player

        

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
