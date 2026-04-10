using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClawsNetworkManager : NetworkManager
{
    [SerializeField] private bool isSteam = false;
    public GameObject playerGameplayPrefab;
    private GameObject playerLobbyPrefab;
    public void SetAddress(string address)
    {
        networkAddress = address;
    }

    public override void ServerChangeScene(string newSceneName)
    {
        if (newSceneName == "GameplayScene")
        {
            playerLobbyPrefab = playerPrefab;
            playerPrefab = playerGameplayPrefab;
            onlineScene = newSceneName;
        }
        base.ServerChangeScene(newSceneName);
    }
}