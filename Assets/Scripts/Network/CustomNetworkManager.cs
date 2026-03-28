using Mirror;
using Steamworks;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
         
        CSteamID steamID = SteamMatchmaking.GetLobbyMemberByIndex(SteamLobby.LobbyID, numPlayers - 1);
        var playerInfoDisplay = conn.identity.GetComponent<PlayerInfoDisplay>();
         
        playerInfoDisplay?.SetSteamId(steamID.m_SteamID);
    }
}
