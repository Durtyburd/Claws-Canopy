using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;

    private bool _isPaused;

    private void Start()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    private void Update()
    {
        bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool startPressed = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

        if (escPressed || startPressed)
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        menuPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        menuPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
