using UnityEngine;

public class PlayerLocator : MonoBehaviour
{
    public static PlayerLocator Instance { get; private set; }
    [SerializeField] Transform playerTransform;

    public Transform Player => playerTransform;

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
