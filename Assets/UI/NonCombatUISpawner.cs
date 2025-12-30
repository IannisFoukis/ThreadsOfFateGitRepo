using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Ensures a NonCombat UI Canvas exists in Room_Entry scenes at runtime.
public class NonCombatUISpawner : MonoBehaviour
{
    const string uiName = "NonCombatUI";

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Room_Entry") return;

        // If already present, just ensure it's active
        var existing = GameObject.Find(uiName);
        if (existing != null)
        {
            existing.SetActive(true);
            return;
        }

        // Create Canvas
        var go = new GameObject(uiName);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        // Add NonCombatUIController
        var ui = go.AddComponent<NonCombatUIController>();

        // Load default card prefab from Resources if present
        var cardPrefab = Resources.Load<GameObject>("DefaultNonCombatCard");
        if (cardPrefab != null)
        {
            ui.cardPrefab = cardPrefab;
        }

        // Create a parent for cards
        var cardParent = new GameObject("CardParent");
        var rt = cardParent.AddComponent<RectTransform>();
        rt.SetParent(go.transform, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(800, 200);

        ui.cardParent = rt;

        // Hide initially; NonCombatUIController.Show will activate and lock input
        go.SetActive(true);
    }
}
