using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Mirror.Examples.SyncDir;
using Steamworks;
using UnityEngine;
using UnityEngine.Serialization;

public class LobbyPlayerList : NetworkBehaviour
{
    [SerializeField] private LobbyPlayerInfo lobbyPlayerInfoPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject roomMenu;
    
    private Dictionary<NetworkConnectionToClient, PlayerConnection> connToPlayerConnection = new Dictionary<NetworkConnectionToClient, PlayerConnection>();
    // private Dictionary<NetworkConnectionToClient, ulong> connQueue = new Dictionary<NetworkConnectionToClient, ulong>();
    public static LobbyPlayerList Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<LobbyPlayerList>(FindObjectsInactive.Include);
            }

            if (_instance == null)
            {
                Debug.LogError("No LobbyPlayerList found");
            }
            
            return _instance;
        }
    }
    
    private static LobbyPlayerList _instance;
    public void RegisterNewPlayer(NetworkConnectionToClient conn, ulong steamID)
    {
        LobbyPlayerInfo lobbyPlayerInfo = Instantiate(lobbyPlayerInfoPrefab);
        lobbyPlayerInfo.SetSteamId(steamID);
        NetworkServer.Spawn(lobbyPlayerInfo.gameObject);
        PlayerConnection playerConnection = new PlayerConnection(steamID, lobbyPlayerInfo);
        connToPlayerConnection[conn] = playerConnection;
    }

    // public void QueuePlayer(NetworkConnectionToClient conn, ulong steamId)
    // {
    //     connQueue.Add(conn, steamId);
    // }
    //
    // [Command(requiresAuthority = false)]
    // public void CmdRegisterPlayers()
    // {
    //     foreach (KeyValuePair<NetworkConnectionToClient,ulong> keyValuePair in connQueue)
    //     {
    //         RegisterNewPlayer(keyValuePair.Key, keyValuePair.Value);
    //     }
    //     connQueue.Clear();
    // }

    public void UnregisterPlayer(NetworkConnectionToClient conn)
    {
        if (connToPlayerConnection.TryGetValue(conn, out PlayerConnection playerConnection))
        {
            if (connToPlayerConnection[conn].lobbyPlayerInfo != null)
            {
                NetworkServer.Destroy(connToPlayerConnection[conn].lobbyPlayerInfo.gameObject);
            }
            connToPlayerConnection.Remove(conn);
        }
        // }else if (connQueue.ContainsKey(conn))
        // {
        //     connQueue.Remove(conn);
        // }
    }

    public void TurnOnRoom()
    {
        roomMenu.gameObject.SetActive(true);
    }
    
    public void LeaveRoom() //TODO: remove script and put all in the lobby menu so i can exit lobby view after exiting room
    {
        roomMenu.gameObject.SetActive(false);
        if (isServer)
        {
            Debug.Log("Host left");
            Clear();
            NetworkManager.singleton.StopHost();
        }
        else
        {
            Debug.Log("Client left");
            NetworkManager.singleton.StopClient();
        }
    }
    
    public void Clear()
    {
        var list = connToPlayerConnection.Keys.ToList();
        foreach (var conn in list)
        {
            // NetworkServer.Destroy(playerConnection.lobbyPlayerInfo.gameObject);
            UnregisterPlayer(conn);
        }
        connToPlayerConnection.Clear();
        content.gameObject.SetActive(false);
    }

    public void RegisterGO(Transform go)
    {
        go.SetParent(content);
    }
}

public class PlayerConnection
{
    public ulong SteamID;
    public LobbyPlayerInfo lobbyPlayerInfo;

    public PlayerConnection(ulong SteamID, LobbyPlayerInfo Lobbyplayerinfo)
    {
        this.SteamID = SteamID;
        this.lobbyPlayerInfo = Lobbyplayerinfo;
    }
}
