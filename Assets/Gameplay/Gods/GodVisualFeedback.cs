using UnityEngine;

public class GodVisualFeedback : MonoBehaviour
{
    SpriteRenderer sr;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = true;
        sr.color = Color.clear;
    }

    // HIGH-LEVEL API (what GodDirector should call)
    public void Show(GodType god)
    {
        Color c = Color.clear;
        float alpha = 0.15f;

        Debug.Log("SHOWING GOD VISUAL: " + god);
        switch (god)
        {
            case GodType.Blood:
                c = new Color(0.6f, 0f, 0f);
                alpha = 0.18f;
                break;

            case GodType.Order:
                c = new Color(0.9f, 0.9f, 0.9f);
                alpha = 0.10f;
                break;

            case GodType.Chaos:
                c = new Color(0.6f, 0f, 0.6f);
                alpha = 0.18f;
                break;

            case GodType.Silence:
                c = Color.black;
                alpha = 0.05f;
                break;
        }

        Show(c, alpha);

    }

    // LOW-LEVEL API (color application)
    public void Show(Color color, float alpha)
    {
        sr.color = new Color(color.r, color.g, color.b, alpha);
    }

    public void Clear()
    {
        sr.color = Color.clear;
    }
}
