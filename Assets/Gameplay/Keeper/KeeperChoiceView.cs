using UnityEngine;
using UnityEngine.UI;

public class KeeperChoiceView : MonoBehaviour
{
    public Text keeperLineText;
    public Button optionAButton;
    public Button optionBButton;
    public Button optionCButton;

    KeeperChoiceData currentChoice;

    public void Show(KeeperChoiceData data)
    {
        currentChoice = data;

        keeperLineText.text = data.keeperLine;

        optionAButton.GetComponentInChildren<Text>().text = data.optionAText;
        optionBButton.GetComponentInChildren<Text>().text = data.optionBText;
        optionCButton.GetComponentInChildren<Text>().text = data.optionCText;

        optionAButton.onClick.RemoveAllListeners();
        optionBButton.onClick.RemoveAllListeners();
        optionCButton.onClick.RemoveAllListeners();

        optionAButton.onClick.AddListener(() => Select(data.optionA));
        optionBButton.onClick.AddListener(() => Select(data.optionB));
        optionCButton.onClick.AddListener(() => Select(data.optionC));

        gameObject.SetActive(true);
    }

    void Select(KeeperChoice choice)
    {
        gameObject.SetActive(false);
        KeeperResolver.ApplyChoice(choice);
        Time.timeScale = 1f;
    }
}
