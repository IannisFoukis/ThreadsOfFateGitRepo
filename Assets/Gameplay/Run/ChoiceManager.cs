using UnityEngine;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance;
    public bool ChoicePending { get; private set; }

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

    public void PresentChoice()
    {
        Debug.Log("PresentChoice() CALLED");
        GameLock.IsLocked = true;
        ChoicePending = true;

        Debug.Log("CHOICE: [C] Accept Corruption | [F] Fight Consequence");
    }


    void Update()
    {
        if (!ChoicePending)
            return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            Accept();
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            Reject();
        }
    }


    void Accept()
    {
        GameLock.IsLocked = false;
        ChoicePending = false;

        RunCorruptionState.Instance.AcceptCorruption();
    }

    void Reject()
    {
        Debug.Log("Reject pressed");

        ChoicePending = false;
        GameLock.IsLocked = false;

        RunCorruptionState.Instance?.RejectCorruption();

        if (EliteSpawner.Instance != null)
        {
            EliteSpawner.Instance.SpawnElite();
            Debug.Log("CONSEQUENCE: Elite Spawned");
        }
        else
        {
            Debug.LogError("EliteSpawner.Instance is NULL — did you forget to add it to Bootstrap?");
        }
    }




    void TriggerFightConsequence()
    {
        Debug.Log("CONSEQUENCE: Extra enemy / harder next room");
        // Step 20: spawn elite / apply modifier
    }
}
