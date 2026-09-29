using UnityEngine;
using UnityEngine.UI;

public class MainMenuTransition : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private string nextSceneName = "SampleScene";
    [SerializeField] private float transitionDuration = 0.8f;
    [SerializeField] private LoadingTips loadingTips;
    [SerializeField, Min(0f)] private float minimumLoadingTime = 2f;
    [SerializeField, Min(0.1f)] private float tipInterval = 4f;

    private bool isTransitioning;

    private void Start()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartGame);
    }

    private void StartGame()
    {
        if (isTransitioning)
            return;

        if (loadingPanel == null ||
            !loadingPanel.TryGetComponent(out LoadingScreenUI loadingUI) || !loadingUI.IsConfigured)
        {
            Debug.LogError("Loading Panel에 LoadingScreenUI를 추가하고 UI 참조를 연결해주세요.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName) || !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"로드할 씬을 Build Profiles의 Scene List에서 확인해주세요: {nextSceneName}", this);
            return;
        }

        isTransitioning = true;
        if (startButton != null)
            startButton.interactable = false;

        GameObject transitionRoot = new GameObject("SceneTransition");
        DontDestroyOnLoad(transitionRoot);

        Canvas canvas = transitionRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = transitionRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        transitionRoot.AddComponent<GraphicRaycaster>();

        GameObject panel = Instantiate(loadingPanel, transitionRoot.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.SetActive(true);

        panel.GetComponent<LoadingScreenUI>().Play(transitionRoot, nextSceneName,
            transitionDuration, minimumLoadingTime, tipInterval, loadingTips);
    }
}
