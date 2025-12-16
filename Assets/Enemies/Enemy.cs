using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static int AliveCount = 0;

    private void OnEnable()
    {
        AliveCount++;
    }

    private void OnDisable()
    {
        AliveCount--;
    }
}
