using UnityEngine;
using UnityEngine.InputSystem;

public class PauseSettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;

    private bool isOpen;
    private float previousTimeScale = 1f;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    private void Awake()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ToggleSettings();
    }

    public void ToggleSettings()
    {
        if (isOpen)
            CloseSettings();
        else
            OpenSettings();
    }

    public void OpenSettings()
    {
        if (isOpen || settingsPanel == null)
            return;

        previousTimeScale = Time.timeScale;
        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        isOpen = true;
        settingsPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseSettings()
    {
        if (!isOpen)
            return;

        isOpen = false;
        settingsPanel.SetActive(false);
        RestoreGameState();
    }

    private void OnDestroy()
    {
        if (isOpen)
            RestoreGameState();
    }

    private void RestoreGameState()
    {
        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
    }
}
