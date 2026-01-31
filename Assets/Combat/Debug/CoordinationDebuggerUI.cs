using UnityEngine;

public class CoordinationDebuggerUI : MonoBehaviour
{
    private EncounterCoordinator coordinator;

    void Awake()
    {
        coordinator = FindFirstObjectByType<EncounterCoordinator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10))
            CoordinationDebug.Enabled = !CoordinationDebug.Enabled;
    }

    void OnGUI()
    {
        if (!CoordinationDebug.Enabled || coordinator == null)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 320, 500));
        GUILayout.Label("=== COORDINATION DEBUG ===");

        foreach (var e in coordinator.GetEnemies())
        {
            if (e == null) continue;

            var roleCtrl = e.GetComponent<EnemyRoleController>();

            string roleText = roleCtrl != null
                ? roleCtrl.currentRole.ToString()
                : "Unknown";

            string slotText = e.assignedSlot >= 0
                ? e.assignedSlot.ToString()
                : "None";

            string text =
                $"{e.name} | Role: {roleText} | Slot: {slotText}";

            GUILayout.Label(text);
        }

        GUILayout.EndArea();
    }
}