using System;
using System.Security.Cryptography;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class TableInteractable : NetworkBehaviour
{
    private bool interacting = false;

    [SyncVar(hook = nameof(OnTableItemChanged))]
    private string tableItem;

    private ItemsManager itemsManagerScript;

    private void Start()
    {
        itemsManagerScript = FindFirstObjectByType<ItemsManager>(FindObjectsInactive.Include);

        InputAction interactAction = InputSystem.actions.FindAction("Interact");
        interactAction.performed += _ =>
        {
            Debug.Log("Interacting");
            interacting = true;
        };
        interactAction.canceled += _ =>
        {
            Debug.Log("Done Interacting");
            interacting = false;
        };

        if (isServer)
        {
            tableItem = "Ball";
            OnTableItemChanged(tableItem, tableItem);            
        } 
    }

    private void Update()
    {
        TestPlayerController[] allPlayers = FindObjectsByType<TestPlayerController>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            GameObject playerObj = player.gameObject;

            if (playerObj && player.isLocalPlayer && player.objPlayerIsNear == gameObject)
            {
                if (interacting)
                {
                    interacting = false;
                    CmdInteractWithTable(playerObj);
                }
            }
        }
    }

    [Command(requiresAuthority = false)]
    void CmdInteractWithTable(GameObject player)
    {
        if (tableItem == "Ball" && player.GetComponent<TestPlayerController>().equippedItem == "")
        {
            player.GetComponent<TestPlayerController>().equippedItem = tableItem;
            tableItem = "";
        }
        else if (tableItem == "" && player.GetComponent<TestPlayerController>().equippedItem == "Ball")
        {
            tableItem = player.GetComponent<TestPlayerController>().equippedItem;
            player.GetComponent<TestPlayerController>().equippedItem = "";
        }
    }

    void OnTableItemChanged(string oldValue, string newValue)
    {
        if (itemsManagerScript)
        {
            foreach (Transform item in transform.Find("InterestParent").transform)
            {
                Destroy(item.gameObject);
            }

            if (newValue != "")
            {
                Transform newObj = Instantiate(itemsManagerScript.itemsListObj.transform.Find(newValue),
                    transform.Find("InterestParent"));
                newObj.transform.name = newValue;
                newObj.gameObject.SetActive(true);
            }
        }
    }
}