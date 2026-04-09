using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    private static SettingsMenu _instance;
    [SerializeField] private RectTransform panel;
    
    private Animator _animator;
    
    public static SettingsMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include);
                if (_instance)
                {
                    _instance._animator = _instance.GetComponent<Animator>();
                }
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
        _animator.SetBool("Show", false);
        settingsToggleButton?.SettingsClosed();
        LobbyMenu.Instance?.SettingsClosed();
    }

    public void OpenMenu()
    {
        _animator.SetBool("Show", true);
    }

}
