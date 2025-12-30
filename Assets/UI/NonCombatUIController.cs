using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Simple non-combat UI controller skeleton - requires designer to hook up prefabs
public class NonCombatUIController : MonoBehaviour
{
    public static NonCombatUIController Instance;

    public GameObject cardPrefab;
    public Transform cardParent;
    public TMPro.TMP_Text headerText;
    public UnityEngine.UI.Image portraitImage;

    Action<int> onChoose;

    void Awake()
    {
        Instance = this;
        EnsureUISetup();
    }

    void Update()
    {
        // If UI not visible do nothing
        if (!gameObject.activeInHierarchy) return;

        // Input handling is centralized in ChoiceManager to avoid duplicate handling
        // NonCombatUIController no longer reads keyboard input.
    }

    void EnsureUISetup()
    {
        // Warn designer if card prefab missing
        if (cardPrefab == null)
            Debug.LogWarning("NonCombatUIController: cardPrefab not assigned in inspector.");

        // Find or create a Canvas in the scene
        if (cardParent == null)
        {
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("NonCombatUIController: No Canvas found in scene. Please add a Canvas and assign cardParent in the inspector.");
                return;
            }

            // Expect designer to provide a NonCombatUI/CardParent hierarchy
            var nc = canvas.transform.Find("NonCombatUI");
            if (nc == null)
            {
                Debug.LogWarning("NonCombatUIController: No 'NonCombatUI' transform found under Canvas. Please add it and assign cardParent.");
                return;
            }

            var cp = nc.Find("CardParent");
            if (cp == null)
            {
                Debug.LogWarning("NonCombatUIController: No 'CardParent' found under NonCombatUI. Please add it and assign cardParent.");
                return;
            }

            cardParent = cp;
            Debug.Log("NonCombatUIController: Assigned cardParent from scene hierarchy.");
        }

        // Ensure EventSystem exists for button input
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
        {
            Debug.LogWarning("NonCombatUIController: No EventSystem found in scene. Please add one to enable UI interactions.");
        }
    }

    public void Show(RoomNPC[] options, Action<int> onChoose, string title = null, Sprite portrait = null)
    {
        ClearCards();
        this.onChoose = onChoose;
        // set header and portrait if provided
        if (headerText != null)
            headerText.text = title ?? string.Empty;

        if (portraitImage != null)
            portraitImage.sprite = portrait;

        for (int i = 0; i < options.Length; i++)
        {
            var opt = options[i];
            var go = Instantiate(cardPrefab, cardParent);
            // Designer must wire Card component to display portrait/name/description
            var card = go.GetComponent<NonCombatCard>();
            if (card != null)
                card.Setup(opt, i, OnCardSelected);
        }

        gameObject.SetActive(true);
        GameLock.IsLocked = true;
        ChoiceManager.Instance?.PresentChoice(onChoose);
    }

    void OnCardSelected(int index)
    {
        GameLock.IsLocked = false;
        if (onChoose == null)
        {
            Debug.LogWarning("NonCombatUIController: onChoose delegate is null when selecting option");
        }
        else
        {
            onChoose.Invoke(index);
        }
        ClearCards();
        gameObject.SetActive(false);
        // Inform ChoiceManager that UI handled closing the choice so rooms can progress
        ChoiceManager.Instance?.FinishChoice();
    }

    // Allow external callers (eg. ChoiceManager keyboard flow) to programmatically choose an option
    public void ChooseOption(int index)
    {
        OnCardSelected(index);
    }

    void ClearCards()
    {
        for (int i = cardParent.childCount - 1; i >= 0; i--)
            Destroy(cardParent.GetChild(i).gameObject);
    }
}
