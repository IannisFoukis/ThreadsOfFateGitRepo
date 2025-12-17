using UnityEngine;

public class JokerVisual : MonoBehaviour
{
    private SpriteRenderer sr;

    void Awake()
    {
        // Look in children, not just root
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (sr != null)
            sr.color = Color.magenta;
    }

    void OnDisable()
    {
        if (sr != null)
            sr.color = Color.white;
    }
}
