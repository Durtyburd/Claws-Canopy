using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class SteamLobby : NetworkBehaviour
{
    public static SteamLobby Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SteamLobby>(FindObjectsInactive.Include);
            }

            if (_instance == null)
            {
                Debug.Log("Couldn't find a Steam Lobby");
            }

            return _instance;
        }
    }

    private static SteamLobby _instance;
    public GameObject hostButton = null;
    public ulong lobbyID;
    public NetworkManager networkManager;
    public Callback<LobbyCreated_t> lobbyCreated;
    public Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    public Callback<LobbyEnter_t> lobbyEntered;
    public Callback<LobbyChatUpdate_t> lobbyChatUpdate;

    private const string HostAddressKey = "HostAddress";

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam is not initialized");
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    }


    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }

    private void OnLobbyCreated(LobbyCreated_t param)
    {
        if (param.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("failed to create lobby " + param.m_eResult.ToString());
            return;
        }

        Debug.Log("created lobby with id: " + param.m_ulSteamIDLobby);
        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(new CSteamID(param.m_ulSteamIDLobby), HostAddressKey,
            SteamUser.GetSteamID().ToString());
        lobbyID = param.m_ulSteamIDLobby;
    }


    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t param)
    {
        Debug.Log("Join requested for lobby " + param.m_steamIDLobby);
        if (NetworkClient.isConnected || NetworkClient.active)
        {
            Debug.Log("Network client is active or connected. disconnecting to join new lobby");
            NetworkManager.singleton.StopClient();
            NetworkClient.Shutdown();
        }

        SteamMatchmaking.JoinLobby(param.m_steamIDLobby);

    }


    private void OnLobbyEntered(LobbyEnter_t param)
    {
        if (NetworkServer.active)
        {
            Debug.Log("Already in a lobby as a host. ignoring join request");
            return;
        }
        lobbyID = param.m_ulSteamIDLobby;
        string _hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(param.m_ulSteamIDLobby), HostAddressKey);
        networkManager.networkAddress = _hostAddress;
        Debug.Log("Entered lobby: " + param.m_ulSteamIDLobby);
        networkManager.StartClient();
        LobbyMenu.Instance.LobbyRoomOn();
        
    }

    void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
        {
            if (callback.m_ulSteamIDLobby != lobbyID) return;

            EChatMemberStateChange stateChange = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;
            Debug.Log($"LobbyChatUpdate: {stateChange}");

            bool shouldUpdate = stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeEntered) ||
                                stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeLeft) ||
                                stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) ||
                                stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeKicked) ||
                                stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeBanned);

            if (shouldUpdate)
            {
                StartCoroutine(DelayedNameUpdate(0.5f));
                LobbyMenu.Instance?.CheckAllPlayersReady();
            }
        }

        private IEnumerator DelayedNameUpdate(float delay)
        {
            if (LobbyMenu.Instance == null)
            {
                Debug.LogWarning("Lobby UI Manager.Instance is null, skipping name update");
                yield break;
            }
            yield return new WaitForSeconds(delay);
            LobbyMenu.Instance?.UpdatePlayerLobbyUI();
        }

        public void LeaveLobby()
        {
            CSteamID currentOwner = SteamMatchmaking.GetLobbyOwner(new CSteamID(lobbyID));
            CSteamID me = SteamUser.GetSteamID();
            var lobby = new CSteamID(lobbyID);
            List<CSteamID> members = new List<CSteamID>();

            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);

            for (int i = 0; i < count; i++)
            {
                members.Add(SteamMatchmaking.GetLobbyMemberByIndex(lobby, i));
            }

            if (lobbyID != 0)
            {
                SteamMatchmaking.LeaveLobby(new CSteamID(lobbyID));
                lobbyID = 0;
            }

            if (NetworkServer.active && currentOwner == me)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
            }

            // panelSwapper.gameObject.SetActive(true);
            this.gameObject.SetActive(true);
            // panelSwapper.SwapPanel("MainPanel");
            LobbyMenu.Instance.LobbyRoomOff();
        }
}