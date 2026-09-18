using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameSettingsUI : MonoBehaviour
{
    private const string MasterMixerParameter = "MasterVolume";
    private const string BgmMixerParameter = "BgmVolume";
    private const string SfxMixerParameter = "SfxVolume";
    private const string AmbienceMixerParameter = "AmbienceVolume";

    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string BgmVolumeKey = "Settings.BgmVolume";
    private const string SfxVolumeKey = "Settings.SfxVolume";
    private const string AmbienceVolumeKey = "Settings.AmbienceVolume";
    private const string SensitivityKey = "Settings.Sensitivity";
    private const string FullScreenKey = "Settings.FullScreen";
    private const string ResolutionWidthKey = "Settings.ResolutionWidth";
    private const string ResolutionHeightKey = "Settings.ResolutionHeight";

    public static float BgmVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;
    public static float AmbienceVolume { get; private set; } = 1f;
    public static float Sensitivity { get; private set; } = 1f;

    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject uiBackground;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject quitConfirmPanel;
    [SerializeField] private Button confirmQuitButton;
    [SerializeField] private Button cancelQuitButton;
    [SerializeField] private Button controlKeyButton;
    [SerializeField] private GameObject controlKeyBackground;

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider ambienceVolumeSlider;
    [SerializeField] private Slider sensitivitySlider;

    [Header("Display")]
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Button applyButton;

    private readonly List<Resolution> resolutions = new List<Resolution>();
    private int controlKeyOpenedFrame = -1;

    private void Start()
    {
        LoadSettings();
        BuildResolutionOptions();
        ConnectEvents();
        settingsPanel.SetActive(false);
        quitConfirmPanel.SetActive(false);
        controlKeyBackground.SetActive(false);
        uiBackground.SetActive(false);
    }

    private void ConnectEvents()
    {
        openButton.onClick.AddListener(OpenSettings);
        closeButton.onClick.AddListener(CloseSettings);
        applyButton.onClick.AddListener(ApplySettings);
        quitButton.onClick.AddListener(OpenQuitConfirm);
        confirmQuitButton.onClick.AddListener(QuitGame);
        cancelQuitButton.onClick.AddListener(CloseQuitConfirm);
        controlKeyButton.onClick.AddListener(ToggleControlKeyBackground);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseTopUI();
            return;
        }

        if (controlKeyBackground.activeSelf && Time.frameCount > controlKeyOpenedFrame && AnyInputPressed())
            controlKeyBackground.SetActive(false);
    }

    private void CloseTopUI()
    {
        if (controlKeyBackground.activeSelf)
        {
            controlKeyBackground.SetActive(false);
            return;
        }

        if (quitConfirmPanel.activeSelf)
        {
            CloseQuitConfirm();
            return;
        }

        if (settingsPanel.activeSelf)
            CloseSettings();
    }

    private bool AnyInputPressed()
    {
        bool keyboardPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame ||
             Mouse.current.rightButton.wasPressedThisFrame ||
             Mouse.current.middleButton.wasPressedThisFrame ||
             Mouse.current.forwardButton.wasPressedThisFrame ||
             Mouse.current.backButton.wasPressedThisFrame);

        return keyboardPressed || mousePressed;
    }

    private void LoadSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        AmbienceVolume = PlayerPrefs.GetFloat(AmbienceVolumeKey, 1f);
        Sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);
        bool fullScreen = PlayerPrefs.GetInt(FullScreenKey, Screen.fullScreen ? 1 : 0) == 1;

        masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        bgmVolumeSlider.SetValueWithoutNotify(BgmVolume);
        sfxVolumeSlider.SetValueWithoutNotify(SfxVolume);
        ambienceVolumeSlider.SetValueWithoutNotify(AmbienceVolume);
        sensitivitySlider.SetValueWithoutNotify(Sensitivity);
        fullScreenToggle.SetIsOnWithoutNotify(fullScreen);

        ApplyMixerVolumes(masterVolume, BgmVolume, SfxVolume, AmbienceVolume);
        Screen.fullScreen = fullScreen;
    }

    private void BuildResolutionOptions()
    {
        resolutions.Clear();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
        int selectedIndex = 0;

        foreach (Resolution resolution in Screen.resolutions)
        {
            bool alreadyAdded = resolutions.Exists(item =>
                item.width == resolution.width && item.height == resolution.height);

            if (alreadyAdded)
                continue;

            resolutions.Add(resolution);
            options.Add($"{resolution.width} x {resolution.height}");

            if (resolution.width == savedWidth && resolution.height == savedHeight)
                selectedIndex = resolutions.Count - 1;
        }

        if (resolutions.Count == 0)
        {
            resolutions.Add(Screen.currentResolution);
            options.Add($"{Screen.width} x {Screen.height}");
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(selectedIndex);
        resolutionDropdown.RefreshShownValue();

        Resolution selectedResolution = resolutions[selectedIndex];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, fullScreenToggle.isOn);
    }

    public void OpenSettings()
    {
        uiBackground.SetActive(true);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        controlKeyBackground.SetActive(false);
        uiBackground.SetActive(false);
    }

    public void ToggleControlKeyBackground()
    {
        bool shouldOpen = !controlKeyBackground.activeSelf;
        controlKeyBackground.SetActive(shouldOpen);

        if (shouldOpen)
            controlKeyOpenedFrame = Time.frameCount;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenQuitConfirm()
    {
        uiBackground.SetActive(true);
        quitConfirmPanel.SetActive(true);
    }

    public void CloseQuitConfirm()
    {
        quitConfirmPanel.SetActive(false);
        uiBackground.SetActive(false);
    }

    public void ApplySettings()
    {
        if (resolutionDropdown.value < 0 || resolutionDropdown.value >= resolutions.Count)
            return;

        float masterVolume = masterVolumeSlider.value;
        BgmVolume = bgmVolumeSlider.value;
        SfxVolume = sfxVolumeSlider.value;
        AmbienceVolume = ambienceVolumeSlider.value;
        Sensitivity = sensitivitySlider.value;
        bool isFullScreen = fullScreenToggle.isOn;
        Resolution resolution = resolutions[resolutionDropdown.value];

        ApplyMixerVolumes(masterVolume, BgmVolume, SfxVolume, AmbienceVolume);
        Screen.SetResolution(resolution.width, resolution.height, fullScreenToggle.isOn);

        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.SetFloat(AmbienceVolumeKey, AmbienceVolume);
        PlayerPrefs.SetFloat(SensitivityKey, Sensitivity);
        PlayerPrefs.SetInt(FullScreenKey, isFullScreen ? 1 : 0);
        PlayerPrefs.SetInt(ResolutionWidthKey, resolution.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, resolution.height);
        PlayerPrefs.Save();
    }

    private void ApplyMixerVolumes(float master, float bgm, float sfx, float ambience)
    {
        if (audioMixer == null)
        {
            Debug.LogError("GameSettingsUI의 Audio Mixer가 연결되지 않았습니다.", this);
            return;
        }

        SetMixerVolume(MasterMixerParameter, master);
        SetMixerVolume(BgmMixerParameter, bgm);
        SetMixerVolume(SfxMixerParameter, sfx);
        SetMixerVolume(AmbienceMixerParameter, ambience);
    }

    private void SetMixerVolume(string parameterName, float volume)
    {
        float decibel = volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;
        audioMixer.SetFloat(parameterName, decibel);
    }
}
