using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LobbyMenu : NetworkBehaviour
{
    List<LobbyButton> buttons;
    int currentIndex = 0;
    private const int inactivePriority = 10;
    const int activePriority = 20;
    private LobbyButton hoveredButton = null;

    public CinemachineCamera lobbyCamera;
    [SerializeField] GameObject roomMenu;
    [SerializeField] Transform buttonsParent;

    public Transform playerListParent;
    public List<TextMeshProUGUI> playerNameTexts = new List<TextMeshProUGUI>();
    public List<PlayerLobbyHandler> playerLobbyHandlers = new List<PlayerLobbyHandler>();
    public List<RawImage> playerImages = new List<RawImage>();
    public Button playGameButton;
    
    public static LobbyMenu Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<LobbyMenu>(FindObjectsInactive.Include);
            }

            if (_instance == null)
            {
                Debug.Log("No LobbyMenu found");
            }
            
            return _instance;
        }
    }

    private static LobbyMenu _instance;
    
    private void Awake()
    {
        if(_instance == null){
            _instance = this;
        }else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        buttons = new List<LobbyButton>();
        
        foreach (Transform child in buttonsParent)
        {
            LobbyButton button = child.GetComponent<LobbyButton>();
            if (button)
            {
                buttons.Add(button);
                // button.myCamera.Priority = inactivePriority;
                Toggle(false);
                button.onHoverEnter = Hovered;
                button.onHoverExit = QuitHovering;
                ++currentIndex;
            }
        }
        currentIndex = 0;
        Toggle(true);
        
        InputSystem.actions.FindAction("Next").performed += _ =>
        {
            SwitchFocus((currentIndex+ 1) % buttons.Count);
        };
        InputSystem.actions.FindAction("Previous").performed += _ =>
        {
            SwitchFocus((currentIndex - 1 + buttons.Count) % buttons.Count);
        };

        InputSystem.actions.FindAction("Submit").performed += _ =>
        {
            if (hoveredButton)
            {
                hoveredButton.Select();
                hoveredButton.onClick.Invoke();
            }
            else
            {
                buttons[currentIndex].Select();
                buttons[currentIndex].onClick.Invoke();
            }
        };
    }

    private void Start()
    {
        playGameButton.interactable = false;
    }

    public void UpdatePlayerLobbyUI()
    {
        playerNameTexts.Clear();
        playerLobbyHandlers.Clear();

        var lobby = new CSteamID(SteamLobby.Instance.lobbyID);
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobby);

        CSteamID hostID = new CSteamID(ulong.Parse(SteamMatchmaking.GetLobbyData(lobby, "HostAddress")));
        List<CSteamID> orderedMembers = new List<CSteamID>();

        if (memberCount == 0)
        {
            Debug.LogWarning("Lobby has no members.. retrying...");
            StartCoroutine(RetryUpdate());
            return;
        }

        orderedMembers.Add(hostID);

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
            if (memberID != hostID)
            {
                orderedMembers.Add(memberID);
            }
        }

        int j = 0;
        foreach (var member in orderedMembers)
        {
            if (j >= playerListParent.childCount)
            {
                Debug.LogWarning($"Child {j} doesn't exist yet, retrying...");
                StartCoroutine(RetryUpdate());
                return;
            }
            PlayerLobbyHandler playerLobbyHandler = playerListParent.GetChild(j).GetComponent<PlayerLobbyHandler>();
            TextMeshProUGUI txtMesh = playerLobbyHandler.nameText;//playerListParent.GetChild(j).GetChild(0).GetComponent<TextMeshProUGUI>();
            RawImage rawImage = playerLobbyHandler.rawImage;
            if (!SteamLobby.Instance.lobbyIcons.ContainsKey(member.m_SteamID))
            {
                int handle = SteamFriends.GetLargeFriendAvatar(member);
                if (handle > 0)
                    SteamLobby.Instance.lobbyIcons[member.m_SteamID] = SteamLobby.Instance.GetSteamImageAsTexture2D(handle, member.m_SteamID);
                else
                {
                    // handle == -1, callback will populate it, retry then
                    StartCoroutine(RetryUpdate());
                    return;
                }
            }
            playerLobbyHandlers.Add(playerLobbyHandler);
            playerNameTexts.Add(txtMesh);
            playerImages.Add(rawImage);

            string playerName = SteamFriends.GetFriendPersonaName(member);
            playerNameTexts[j].text = playerName;
            rawImage.texture = SteamLobby.Instance.lobbyIcons[member.m_SteamID];
            j++;
        }
    }

    public void OnPlayButtonClicked()
    {
        if (NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene("GameplayScene"); //propagates to clients automatically?
        }
    }

    public void RegisterPlayer(PlayerLobbyHandler player)
    {
        player.transform.SetParent(playerListParent, false);
        UpdatePlayerLobbyUI();
    }

    [Server]
    public void CheckAllPlayersReady()
    {
        foreach (var player in playerLobbyHandlers)
        {
            if (!player.isReady)
            {
                RpcSetPlayButtonInteractable(false);
                return;
            }
        }
        RpcSetPlayButtonInteractable(true);
    }

    [ClientRpc]
    void RpcSetPlayButtonInteractable(bool truthStatus)
    {
        playGameButton.interactable = truthStatus;
    }

    private IEnumerator RetryUpdate()
    {
        yield return new WaitForSeconds(1f);
        UpdatePlayerLobbyUI();
    }

    private void SwitchFocus(int newIndex)
    {
        Toggle(false);
        currentIndex = newIndex;
        Toggle(true);
    }

    private void Toggle(bool on, bool moveCamera = true)
    {
        buttons[currentIndex].myDinosaurAnimator.SetBool("isSelected", on);
        if (moveCamera)
        {
            buttons[currentIndex].myCamera.Priority = on ? activePriority : inactivePriority;
        }
        buttons[currentIndex].interactable = on;
    }
    
    private void Hovered(LobbyButton hovered)
    {
        int index = buttons.IndexOf(hovered);
        int tmp = currentIndex;
        Toggle(false, false);
        currentIndex = index;
        hoveredButton = hovered;
        Toggle(true, false);
        currentIndex = tmp;
    }

    private void QuitHovering(LobbyButton hovered)
    {
        hoveredButton = null;
        int index = buttons.IndexOf(hovered);
        if (currentIndex != index)
        {
            int tmp = currentIndex;
            currentIndex = index;
            Toggle(false, false);
            currentIndex = tmp;
            if (!SettingsMenu.Instance.enabled)
            {
                Toggle(true, false);
            }
        }
    }

    public void LobbyRoomOn() 
    {
        lobbyCamera.Priority = activePriority + 1;
        buttons[currentIndex].myDinosaurAnimator.SetBool("isRoaring", true);
        roomMenu.gameObject.SetActive(true);
    }

    public void LobbyRoomOff()
    {
        lobbyCamera.Priority = inactivePriority;
        buttons[currentIndex].myDinosaurAnimator.SetBool("isRoaring", false);
        roomMenu.gameObject.SetActive(false);
    }

    public void SettingsClosed()
    {
        Toggle(true);
    }

    public void StartGame()
    {
        
    }
}
