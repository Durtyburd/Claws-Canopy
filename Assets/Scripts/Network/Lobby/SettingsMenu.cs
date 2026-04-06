using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    private static SettingsMenu _instance;
    public LobbyMenu lobbyMenu;
    
    public static SettingsMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include);
            }

            if (_instance == null)
            {
                Debug.LogError("No settings menu found");
            }
            return _instance;
        }
    }
    
    public LobbyButton settingsToggleButton;

    public void CloseMenu()
    {
        this.gameObject.SetActive(false);
        settingsToggleButton?.SettingsClosed();
        lobbyMenu.SettingsClosed();
    }

    public void OpenMenu()
    {
        this.gameObject.SetActive(true);
    }
}
