using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeeperPronouncement : MonoBehaviour
{
    [SerializeField] TMP_Text pronouncementText;
    [SerializeField] float fadeInTime = 0.5f;

    CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Show(string text)
    {
        pronouncementText.text = text;
        gameObject.SetActive(true);
        StartCoroutine(FadeIn());
    }

    System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeInTime);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
    }
    public void Clear()
    {
        gameObject.SetActive(false);
    }

}
