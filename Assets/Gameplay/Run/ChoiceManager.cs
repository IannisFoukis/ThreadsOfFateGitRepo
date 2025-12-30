using UnityEngine;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance;
    public bool ChoicePending { get; private set; }

    // When UI presents a choice we capture its callback so keyboard input
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
        GameLock.IsLocked = true;
        ChoicePending = true;
        // Capture the provided callback (preferred) so keyboard flow can invoke it directly
        capturedCallback = callback;
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
                    if (capturedCallback != null)
                    {
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
        ChoicePending = false;
        GameLock.IsLocked = false;

        RunCorruptionState.Instance?.RejectCorruption();
        GodDirector.Instance?.EvaluateRun();

        if (EliteSpawner.Instance != null)
        {
            EliteSpawner.Instance.SpawnElite();
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

    // Reflection fallback removed — UI callback or direct UI invocation should be used.
}
