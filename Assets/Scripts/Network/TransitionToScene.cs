using System;
using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;
using System.IO;

public class TransitionToScene : NetworkBehaviour
{
    private CustomNetworkManager CustomNetworkManagerScript;
    private FadeInOutScreen fadeInOutScreenScript;


    [Scene] public string transitionToSceneName;
    public string scenePosToSpawnOn;


    private void Awake()
    {
        if (CustomNetworkManagerScript == null)
        {
            CustomNetworkManagerScript = FindObjectOfType<CustomNetworkManager>();
            fadeInOutScreenScript = FindObjectOfType<FadeInOutScreen>();
        }
    }


    private void OnTriggerEnter(Collider collision)
    {
        if (collision.GetComponent<TestPlayerController>())
        {
            if (gameObject.scene != collision.gameObject.scene)
            {
                Debug.Log("wrong scene return");
                return;
            }
            if (collision.TryGetComponent<TestPlayerController>(out TestPlayerController playerMoveScript))
            {
                playerMoveScript.enabled = false;
            }

            if (isServer)
            {
                StartCoroutine(SendPlayerToNewScene(collision.gameObject));
            }
        }
    }


    [ServerCallback]
    IEnumerator SendPlayerToNewScene(GameObject player)
    {
        if (player.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity))
        {
            NetworkConnectionToClient conn = identity.connectionToClient;
            if (conn == null) yield break;


            conn.Send(new SceneMessage
            {
                sceneName = this.gameObject.scene.path, sceneOperation = SceneOperation.UnloadAdditive,
                customHandling = true
            });


            yield return new WaitForSeconds((fadeInOutScreenScript.speed * 0.1f));

            NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.Unspawn); //, false);


            NetworkStartPosition[] allStartPos = FindObjectsOfType<NetworkStartPosition>();

            Transform start = CustomNetworkManagerScript.GetStartPosition();
            foreach (var item in allStartPos)
            {
                if (item.gameObject.scene.name == Path.GetFileNameWithoutExtension(transitionToSceneName) &&
                    item.name == scenePosToSpawnOn)
                {
                    start = item.transform;
                }
            }

            player.transform.position = start.position;


            SceneManager.MoveGameObjectToScene(player, SceneManager.GetSceneByPath(transitionToSceneName));

            conn.Send(new SceneMessage
            {
                sceneName = transitionToSceneName, sceneOperation = SceneOperation.LoadAdditive, customHandling = true
            });


            NetworkServer.AddPlayerForConnection(conn, player);


            if (NetworkClient.localPlayer != null &&
                NetworkClient.localPlayer.TryGetComponent<TestPlayerController>(
                    out TestPlayerController playerMoveScript))
            {
                playerMoveScript.enabled = true;
            }
        }
    }
}