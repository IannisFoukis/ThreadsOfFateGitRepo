using TMPro;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class KeeperPronouncement : MonoBehaviour
{
    [SerializeField] TMP_Text pronouncementText;
    [SerializeField] float fadeInTime = 0.5f;
    [SerializeField] float visibleTime = 3f;
    [SerializeField] float fadeOutTime = 0.5f;

    [SerializeField] AudioSource whisperAudio;


    CanvasGroup canvasGroup;
    Coroutine currentRoutine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows a single keeper line, fades in, waits, fades out.
    /// Safe to call multiple times.
    /// </summary>
    public void Show(string text)
    {
        if (pronouncementText == null || string.IsNullOrEmpty(text))
            return;

        pronouncementText.text = text;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        gameObject.SetActive(true);

        // Phase E: play sound ONLY when Keeper speaks
        if (whisperAudio != null)
            whisperAudio.Play();

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        // Fade in
        float t = 0f;
        canvasGroup.alpha = 0f;

        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeInTime);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        // Stay visible
        yield return new WaitForSecondsRealtime(visibleTime);

        // Fade out
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeOutTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        currentRoutine = null;
    }

    /// <summary>
    /// Hard hide (used when starting a new run or scene).
    /// </summary>
    public void Clear()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        currentRoutine = null;
    }
    public void Hide()
    {
        Clear();
    }

}
