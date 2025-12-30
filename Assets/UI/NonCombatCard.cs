using UnityEngine;
using UnityEngine.UI;

// Simple UI card component to be attached to cardPrefab
public class NonCombatCard : MonoBehaviour
{
    public Image portraitImage;
    public TMPro.TMP_Text nameText;
    public TMPro.TMP_Text descriptionText;
    int index;
    RoomNPC data;

    public RoomNPC Data => data;

    public void Setup(RoomNPC npc, int index, System.Action<int> onSelected)
    {
        this.index = index;
        data = npc;
        if (portraitImage != null && npc.portrait != null) portraitImage.sprite = npc.portrait;
        if (nameText != null) nameText.text = npc.displayName;
        if (descriptionText != null) descriptionText.text = npc.description;

        // wire button (search children as well)
        var btn = GetComponentInChildren<UnityEngine.UI.Button>();
        if (btn != null)
        {
            // remove any existing listeners from pooled or reused prefabs
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onSelected?.Invoke(index));
        }
    }

    void OnDestroy()
    {
        var btn = GetComponentInChildren<UnityEngine.UI.Button>();
        if (btn != null)
            btn.onClick.RemoveAllListeners();
    }
}
