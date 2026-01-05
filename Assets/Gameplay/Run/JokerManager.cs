using UnityEngine;

public class JokerManager : MonoBehaviour
{
    public static JokerManager Instance;

    public bool jokerActive = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AssignJoker(GameObject enemy)
    {
        enemy.AddComponent<Joker>();
        enemy.AddComponent<JokerVisual>();

        Debug.Log("JOKER ASSIGNED");
    }



    public void OnJokerKilled()
    {
        Debug.Log("JokerManager.OnJokerKilled()");

        jokerActive = false;

        if (RunContext.Instance != null)
        {
            RunContext.Instance.memory.jokerKilled = true;
            Debug.Log("[Joker] Joker killed — memory recorded");
        }
        else
        {
            Debug.LogError("[Joker] RunContext missing when Joker killed");
        }
    }


   

}
