using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private GameObject loadingContent;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TMP_Text tipText;

    public bool IsConfigured => transform is RectTransform && fadeImage != null &&
        loadingContent != null && loadingBar != null && tipText != null &&
        fadeImage.transform.IsChildOf(transform) && loadingContent.transform.IsChildOf(transform) &&
        loadingContent != gameObject && !fadeImage.transform.IsChildOf(loadingContent.transform) &&
        loadingBar.transform.IsChildOf(loadingContent.transform) &&
        tipText.transform.IsChildOf(loadingContent.transform);

    public void Play(GameObject transitionRoot, string sceneName, float fadeDuration,
        float minimumLoadingTime, float tipInterval, LoadingTips tips)
    {
        StartCoroutine(LoadScene(transitionRoot, sceneName, fadeDuration,
            minimumLoadingTime, tipInterval, tips));
    }

    private IEnumerator LoadScene(GameObject transitionRoot, string sceneName, float fadeDuration,
        float minimumLoadingTime, float tipInterval, LoadingTips tips)
    {
        loadingContent.SetActive(false);
        fadeImage.raycastTarget = true;
        yield return Fade(0f, 1f, fadeDuration);

        loadingBar.minValue = 0f;
        loadingBar.maxValue = 1f;
        loadingBar.wholeNumbers = false;
        loadingBar.interactable = false;
        loadingBar.SetValueWithoutNotify(0f);
        int tipIndex = tips != null && tips.Count > 0 ? Random.Range(0, tips.Count) : -1;
        tipText.text = tips != null ? tips.GetTip(tipIndex) : string.Empty;
        loadingContent.SetActive(true);
        yield return null;

        AsyncOperation loading = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        loading.allowSceneActivation = false;
        float elapsed = 0f;
        float tipElapsed = 0f;
        while (loading.progress < 0.9f || elapsed < minimumLoadingTime)
        {
            elapsed += Time.unscaledDeltaTime;
            tipElapsed += Time.unscaledDeltaTime;
            loadingBar.SetValueWithoutNotify(Mathf.Clamp01(loading.progress / 0.9f));

            if (tips != null && tips.Count > 1 && tipElapsed >= Mathf.Max(0.1f, tipInterval))
            {
                tipIndex = (tipIndex + Random.Range(1, tips.Count)) % tips.Count;
                tipText.text = tips.GetTip(tipIndex);
                tipElapsed = 0f;
            }
            yield return null;
        }

        loadingBar.SetValueWithoutNotify(1f);
        yield return null;
        loading.allowSceneActivation = true;
        while (!loading.isDone)
            yield return null;

        yield return null;
        loadingContent.SetActive(false);
        yield return Fade(1f, 0f, fadeDuration);
        Destroy(transitionRoot);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;
        color.a = from;
        fadeImage.color = color;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeImage.color = color;
            yield return null;
        }
        color.a = to;
        fadeImage.color = color;
    }
}
