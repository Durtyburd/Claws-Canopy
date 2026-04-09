using System;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerInfo : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandleSteamIdUpdated))]
    private ulong steamId = 123412;
    
    [SerializeField] private GameObject displayGO;
    [SerializeField] RawImage rawImage = null;
    [SerializeField] private TMP_Text nameText = null;
    protected Callback<AvatarImageLoaded_t> avatarImageLoaded;
    
    #region Server

    public void SetSteamId(ulong steamId)
    {
        Debug.Log("Steam id set");
        this.steamId = steamId;
    }

    #endregion

    #region Client

    // LobbyPlayerInfo.cs

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Always initialize when the object becomes active on any client
        Initialize();
    
        // If steamId already has a value (set before we joined), apply it now
        if (steamId != 0)
        {
            HandleSteamIdUpdated(0, steamId);
        }
    }

    void Initialize()
    {
        Debug.Log("Client initialized");
        LobbyPlayerList.Instance.RegisterGO(displayGO.transform);
        avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        Debug.Log("avatar loaded");
        if (callback.m_steamID.m_SteamID != this.steamId)
        {
            return;
        }
        
        rawImage.texture = GetSteamImageAsTexture2D(callback.m_iImage);
    }
    
    private void HandleSteamIdUpdated(ulong oldId, ulong newId)
    {
        if (avatarImageLoaded == null)
        {
            Initialize();
        }
        Debug.Log("HandleSteamIdUpdated");

        CSteamID cSteamId = new CSteamID(newId);
        nameText.text = SteamFriends.GetFriendPersonaName(cSteamId);

        int imageId = SteamFriends.GetLargeFriendAvatar(cSteamId);
        if (imageId == -1)
        {
            return;
        }

        rawImage.texture = GetSteamImageAsTexture2D(imageId);
    }

    private Texture2D GetSteamImageAsTexture2D(int iImage)
    {
        Texture2D texture = null;

        bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);
        if (isValid)
        {
            byte[] image = new byte[width * height * 4];

            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));
            if (isValid)
            {
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(image);
                texture.Apply();
            }
        }
        else
        {
            Destroy(gameObject);
            return null;
        }
        return texture;
    }


    private void OnDestroy()
    {
        Destroy(displayGO);
    }

    #endregion
}
