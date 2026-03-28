using System;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class SteamLobby : MonoBehaviour
{
    NetworkManager networkManager;
    public GameObject hostButton;
    
    Callback<LobbyCreated_t> lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private Callback<LobbyEnter_t> lobbyEntered;
    
    public const string HostAddressKey ="HostAddress";
    public static CSteamID LobbyID { get; private set; }

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();
        if (!SteamManager.Initialized)
        {
            Debug.Log("Steam manager is not initialized");
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        
        // HostLobby();
    }

    public void HostLobby()
    {
        hostButton.SetActive(false);
        
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active)
        {
            return;
        }
        
        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);
        
        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();
        
        hostButton.SetActive(false);
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            hostButton.SetActive(true);
            return;
        }

        LobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(LobbyID, HostAddressKey,
            SteamUser.GetSteamID().ToString());
    }
}
