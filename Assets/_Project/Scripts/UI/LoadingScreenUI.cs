using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    public static bool IsLoading { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetLoading() => IsLoading = false;
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

    public void ShowMenuLoading(GameObject root, Rubber.UI.VillaMenuBackground background, LoadingTips tips)
    {
        IsLoading = true;
        fadeImage.color = new Color(fadeImage.color.r,fadeImage.color.g,fadeImage.color.b,1);
        fadeImage.raycastTarget = true;
        loadingContent.SetActive(true);
        loadingBar.minValue=0;loadingBar.maxValue=1;loadingBar.wholeNumbers=false;loadingBar.interactable=false;
        loadingBar.SetValueWithoutNotify(0);
        tipText.text=tips != null ? tips.GetTip(0) : "Preparing the villa...";
        StartCoroutine(FinishMenuLoading(root,background));
    }
    private IEnumerator FinishMenuLoading(GameObject root, Rubber.UI.VillaMenuBackground background)
    {
        while(background && !background.Ready){loadingBar.SetValueWithoutNotify(background.LoadProgress);yield return null;}
        loadingBar.SetValueWithoutNotify(1);
        yield return null;
        loadingContent.SetActive(false);
        yield return Fade(1,0,.6f);
        IsLoading=false;Destroy(root);
    }

    public void Play(GameObject transitionRoot, string sceneName, float fadeDuration,
        float minimumLoadingTime, float tipInterval, LoadingTips tips)
    {
        IsLoading = true;
        Time.timeScale = 1f;
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

        var menuBackground = FindAnyObjectByType<Rubber.UI.VillaMenuBackground>();
        if (menuBackground != null) yield return menuBackground.PrepareForGame();
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

        loadingBar.SetValueWithoutNotify(sceneName == "MainMenu" ? .9f : 1f);
        yield return null;
        loading.allowSceneActivation = true;
        while (!loading.isDone)
            yield return null;

        yield return null;
        var destinationBackground=FindAnyObjectByType<Rubber.UI.VillaMenuBackground>();
        while(destinationBackground && !destinationBackground.Ready)
        {
            loadingBar.SetValueWithoutNotify(.9f + .1f * destinationBackground.LoadProgress);
            yield return null;
        }
        loadingBar.SetValueWithoutNotify(1);
        loadingContent.SetActive(false);
        yield return Fade(1f, 0f, fadeDuration);
        IsLoading = false;
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
