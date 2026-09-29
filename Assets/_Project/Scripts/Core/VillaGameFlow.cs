using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Rubber.Gameplay.Ducks;
using Rubber.Gameplay.Player;
using Rubber.Gameplay.Interaction;
namespace Rubber.Core
{
    public sealed class VillaGameFlow : MonoBehaviour
    {
        public enum Phase { Playing, Paused, Complete, Loading }
        public RubberDuckReturnRegistry registry;
        public PlayerInputReader input;
        public PlayerMovement movement;
        public PlayerInteractionDetector detector;
        public GameObject reticle, pausePanel, completePanel;
        public TMP_Text counter, completionText;
        public GameSettingsUI settings;
        public GameObject loadingPanel;
        public LoadingTips tips;
        public Phase CurrentPhase { get; private set; }
        public float ElapsedSeconds { get; private set; }
        void Start()
        {
            Time.timeScale=1;CurrentPhase=Phase.Playing;
            input.PauseHandledExternally=true;
            pausePanel.SetActive(false);completePanel.SetActive(false);
            registry.ProgressChanged+=Progress;registry.AllDucksReturned+=Complete;
            settings.SettingsClosed+=OnSettingsClosed;
            Progress(registry.CurrentProgress);SetControl(!LoadingScreenUI.IsLoading);
        }
        void Update()
        {
            if(CurrentPhase==Phase.Loading)return;
            if(LoadingScreenUI.IsLoading){SetControl(false);return;}
            if(CurrentPhase==Phase.Playing){if(!input.enabled)SetControl(true);ElapsedSeconds+=Time.deltaTime;}
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if(CurrentPhase==Phase.Playing)Pause();
                else if(CurrentPhase==Phase.Paused){if(settings.IsSettingsOpen)settings.HandleBack();else Resume();}
            }
        }
        void Progress(DuckCollectionProgress p)=>counter.text=$"{p.ReturnedCount:00} / {p.TotalCount:00}";
        void SetControl(bool enabled)
        {
            if(input.enabled!=enabled)input.enabled=enabled;
            detector.enabled=enabled;reticle.SetActive(enabled);
            if(!enabled){movement.ClearInput();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        }
        public void Pause()
        {
            if(CurrentPhase!=Phase.Playing||LoadingScreenUI.IsLoading)return;
            CurrentPhase=Phase.Paused;SetControl(false);Time.timeScale=0;pausePanel.SetActive(true);
        }
        public void Resume()
        {
            if(CurrentPhase!=Phase.Paused)return;
            settings.CloseSettings();pausePanel.SetActive(false);Time.timeScale=1;CurrentPhase=Phase.Playing;SetControl(true);
        }
        public void OpenSettings(){if(CurrentPhase!=Phase.Paused)return;pausePanel.SetActive(false);settings.OpenSettings();}
        void OnSettingsClosed(){if(CurrentPhase==Phase.Paused)pausePanel.SetActive(true);}
        void Complete()
        {
            if(CurrentPhase==Phase.Complete||CurrentPhase==Phase.Loading)return;
            CurrentPhase=Phase.Complete;settings.CloseSettings();pausePanel.SetActive(false);SetControl(false);Time.timeScale=0;
            int seconds=Mathf.FloorToInt(ElapsedSeconds);
            completionText.text=$"All {registry.TotalDuckCount} ducks are home!\n{seconds/60:00}:{seconds%60:00}";completePanel.SetActive(true);
        }
        public void Restart()=>Navigate("Modern Villa");
        public void ReturnToMenu()=>Navigate("MainMenu");
        void Navigate(string destination)
        {
            if(CurrentPhase==Phase.Loading||LoadingScreenUI.IsLoading)return;
            if(!Application.CanStreamedLevelBeLoaded(destination)||!loadingPanel.GetComponent<LoadingScreenUI>().IsConfigured){Debug.LogError("Scene transition is not configured.",this);return;}
            CurrentPhase=Phase.Loading;settings.CloseSettings();pausePanel.SetActive(false);completePanel.SetActive(false);SetControl(false);Time.timeScale=1;
            var root=new GameObject("SceneTransition");DontDestroyOnLoad(root);
            var canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
            var scaler=root.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;root.AddComponent<GraphicRaycaster>();
            var panel=Instantiate(loadingPanel,root.transform);var rect=(RectTransform)panel.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;panel.SetActive(true);
            panel.GetComponent<LoadingScreenUI>().Play(root,destination,.4f,1f,4f,tips);
        }
        void OnDestroy()
        {
            if(registry){registry.ProgressChanged-=Progress;registry.AllDucksReturned-=Complete;}
            if(settings)settings.SettingsClosed-=OnSettingsClosed;
            Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
    }
}
