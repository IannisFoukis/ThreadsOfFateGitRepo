using UnityEngine;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance;
    public bool ChoicePending { get; private set; }

    // When UI presents a choice we try to capture its callback so keyboard input
    // can directly invoke the same delegate even if the UI instance's field is cleared
    System.Action<int> capturedCallback;

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

    public void PresentChoice(System.Action<int> callback = null)
    {
        Debug.Log("PresentChoice() CALLED");
        GameLock.IsLocked = true;
        ChoicePending = true;
        Debug.Log("CHOICE: [C] Accept Corruption | [F] Fight Consequence");
        // Capture the provided callback (preferred) so keyboard flow can invoke it directly
        capturedCallback = callback;
        Debug.Log($"ChoiceManager: captured UI callback={(capturedCallback == null ? "NULL" : capturedCallback.Method.Name)}");
    }


    void Update()
    {
        if (!ChoicePending)
            return;

        // If a NonCombat UI exists, allow numeric and keeper-style keys to select options
        var ui = NonCombatUIController.Instance;
        if (ui != null && ui.gameObject.activeInHierarchy)
        {
            // numeric quick-select (1-based)
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    Debug.Log($"ChoiceManager: forwarding numeric key {i+1} to NonCombatUIController (Instance={ui.GetInstanceID()})");
                    if (capturedCallback != null)
                    {
                        Debug.Log($"ChoiceManager: invoking capturedCallback for option {i}");
                        capturedCallback.Invoke(i);
                        FinishChoice();
                    }
                    else
                    {
                        ui.ChooseOption(i);
                    }
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log($"ChoiceManager: forwarding C to NonCombatUIController (Instance={ui.GetInstanceID()}) -> option 0");
                if (capturedCallback != null)
                {
                    capturedCallback.Invoke(0);
                    FinishChoice();
                }
                else
                {
                    ui.ChooseOption(0);
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log($"ChoiceManager: forwarding L to NonCombatUIController (Instance={ui.GetInstanceID()}) -> option 1");
                if (capturedCallback != null)
                {
                    capturedCallback.Invoke(1);
                    FinishChoice();
                }
                else
                {
                    ui.ChooseOption(1);
                }
                return;
            }
        }

        // Fallback: direct accept/reject flow when no UI is present
        if (Input.GetKeyDown(KeyCode.C))
        {
            Object.FindAnyObjectByType<RoomDemandTracker>()?.OnCorruptionAccepted();

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
        GodDirector.Instance?.EvaluateRun();

    }

    void Reject()
    {
        Debug.Log("Reject pressed");

        ChoicePending = false;
        GameLock.IsLocked = false;

        RunCorruptionState.Instance?.RejectCorruption();
        GodDirector.Instance?.EvaluateRun();

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

    // Called by external UI flows to end the current choice state when the selection
    // was handled outside of ChoiceManager (eg. NonCombatUIController).
    public void FinishChoice()
    {
        ChoicePending = false;
        GameLock.IsLocked = false;
    }





    void TriggerFightConsequence()
    {
        Debug.Log("CONSEQUENCE: Extra enemy / harder next room");
        // Step 20: spawn elite / apply modifier
    }
}
