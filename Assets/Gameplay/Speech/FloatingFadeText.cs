using UnityEngine;
using TMPro;

public class FloatingFadeText : MonoBehaviour
{
    public float lifetime = 1.2f;
    public float riseDistance = 0.4f;
    public AnimationCurve alphaCurve =
        AnimationCurve.EaseInOut(0, 1, 1, 0);

    private TextMeshPro text;
    private Vector3 startPos;
    private float timer;

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
        startPos = transform.position;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;

        // Rise
        transform.position = startPos + Vector3.up * riseDistance * t;

        float scalePop = Mathf.Lerp(1.05f, 1f, t);
        transform.localScale = Vector3.one * scalePop;

        // Fade
        Color c = text.color;
        c.a = alphaCurve.Evaluate(t);
        text.color = c;

        if (t >= 1f)
            Destroy(gameObject);
    }
}
