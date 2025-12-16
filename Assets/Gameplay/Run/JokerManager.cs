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
        if (jokerActive) return;

        enemy.AddComponent<Joker>();
        jokerActive = true;

        Debug.Log("JOKER ASSIGNED");
    }

    public void OnJokerKilled()
    {
        Debug.Log("JokerManager.OnJokerKilled()");
        jokerActive = false;
        TriggerChoice();
    }

    void TriggerChoice()
    {
        Debug.Log("TriggerChoice()");
        Debug.Log("JOKER KILLED — PLAYER MUST CHOOSE");
        ChoiceManager.Instance.PresentChoice();
    }

}
