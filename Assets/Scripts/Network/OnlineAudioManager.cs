using UnityEngine;

public class OnlineAudioManager : MonoBehaviour
{
    private static OnlineAudioManager _instance;

    public static OnlineAudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<OnlineAudioManager>();
            }

            if (_instance == null)
            {
                Debug.LogError("There is no instance of OnlineAudioManager!");
            }
            return _instance;
        }
    }
    
    [SerializeField] GameObject lobbyListener;

    public void EndLobbySound()
    {
        Destroy(lobbyListener);
    }
}
