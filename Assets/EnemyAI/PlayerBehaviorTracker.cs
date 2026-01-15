using UnityEngine;

public class PlayerBehaviorTracker : MonoBehaviour
{
    [Header("Dash Tracking")]
    [SerializeField] private float dashWindow = 3f;

    private float[] dashTimes = new float[32];
    private int dashWriteIndex;

    public Vector2 LastMoveDir { get; private set; } = Vector2.right;

    private void Update()
    {
        Debug.Log($"[PlayerBehavior] Dash rate: {GetDashRate()}");

    }
    public void RegisterDash()
    {
        dashTimes[dashWriteIndex] = Time.time;
        dashWriteIndex = (dashWriteIndex + 1) % dashTimes.Length;
    }

    public float GetDashRate()
    {
        float now = Time.time;
        int count = 0;

        for (int i = 0; i < dashTimes.Length; i++)
        {
            float t = dashTimes[i];
            if (t > 0f && (now - t) <= dashWindow)
                count++;
        }

        return count / Mathf.Max(0.01f, dashWindow);
    }

    public bool IsDashSpamming(float thresholdPerSecond = 1.0f)
    {
        return GetDashRate() >= thresholdPerSecond;
    }

    public void RegisterMove(Vector2 inputDir)
    {
        if (inputDir.sqrMagnitude > 0.001f)
            LastMoveDir = inputDir.normalized;
    }
}
