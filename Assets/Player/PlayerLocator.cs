using UnityEngine;

public class PlayerLocator : MonoBehaviour
{
    public static PlayerLocator Instance { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}
