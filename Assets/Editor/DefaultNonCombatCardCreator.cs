#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

[InitializeOnLoad]
public static class DefaultNonCombatCardCreator
{
    const string resourcesDir = "Assets/Resources";
    const string prefabPath = resourcesDir + "/DefaultNonCombatCard.prefab";

    static DefaultNonCombatCardCreator()
    {
        // Defer creation until editor is fully initialized to avoid timing issues
        UnityEditor.EditorApplication.delayCall += CreateIfMissing;
    }

    [MenuItem("Tools/Create Default NonCombat Card Prefab (Resources)")]
    public static void CreateIfMissing()
    {
        if (File.Exists(prefabPath))
        {
            Debug.Log("DefaultNonCombatCard.prefab already exists at Assets/Resources/DefaultNonCombatCard.prefab");
            return;
        }

        if (!Directory.Exists(resourcesDir))
            Directory.CreateDirectory(resourcesDir);

        // Root
        var root = new GameObject("DefaultNonCombatCard");
        var rect = root.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 120);

        var img = root.AddComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);

        var btn = root.AddComponent<Button>();

        // NonCombatCard
        var card = root.AddComponent<NonCombatCard>();

        // Portrait
        var portraitGO = new GameObject("Portrait");
        portraitGO.transform.SetParent(root.transform, false);
        var pRt = portraitGO.AddComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0f, 0.5f);
        pRt.anchorMax = new Vector2(0f, 0.5f);
        pRt.anchoredPosition = new Vector2(40, 0);
        pRt.sizeDelta = new Vector2(64, 64);
        var portraitImg = portraitGO.AddComponent<Image>();
        portraitImg.color = Color.white;

        // Name
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(root.transform, false);
        var nRt = nameGO.AddComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0f, 1f);
        nRt.anchorMax = new Vector2(1f, 1f);
        nRt.pivot = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(80, -10);
        nRt.sizeDelta = new Vector2(200, 30);
        var nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = "Name";
        nameText.fontSize = 20;
        nameText.color = Color.white;

        // Description
        var descGO = new GameObject("Description");
        descGO.transform.SetParent(root.transform, false);
        var dRt = descGO.AddComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 0.5f);
        dRt.pivot = new Vector2(0f, 0f);
        dRt.anchoredPosition = new Vector2(80, 10);
        dRt.sizeDelta = new Vector2(200, 60);
        var descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.text = "Description";
        descText.fontSize = 14;
        descText.color = Color.white;

        // Assign references on NonCombatCard
        card.portraitImage = portraitImg;
        card.nameText = nameText;
        card.descriptionText = descText;

        // Save as prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        if (prefab != null)
            Debug.Log("Created default NonCombat card prefab at " + prefabPath);
        else
            Debug.LogError("Failed to create default NonCombat card prefab");

        // Cleanup
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
