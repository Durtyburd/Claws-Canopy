using Mirror;
using UnityEngine;

public class PlayerList : MonoBehaviour
{
    //add players to list and spawn their image
    public static PlayerList Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PlayerList>();
            }

            if (_instance == null)
            {
                Debug.LogError($"No Player List could be found.");
            }
            return _instance;
        }
    }
    
    private static PlayerList _instance;

    public void RegisterPlayer(Transform player)
    {
        player.rotation = Quaternion.identity;
        player.SetParent(transform);
        player.localPosition = Vector3.zero;
    }
}