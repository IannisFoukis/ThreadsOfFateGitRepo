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
        if (!CoordinationDebug.Enabled || coordinator == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        GUILayout.Label("=== COORDINATION DEBUG ===");

        foreach (var e in coordinator.GetEnemies())
        {
            var roleCtrl = e.GetComponent<EnemyRoleController>();

            string roleText = roleCtrl != null
                ? roleCtrl.currentRole.ToString()
                : "Unknown";

            string text =
                $"{e.name} | Role: {roleText} | " +
                $"Slot: {(e.assignedSlot.HasValue ? e.assignedSlot.ToString() : "None")}";

            GUILayout.Label(text);
        }

        GUILayout.EndArea();
    }

}
