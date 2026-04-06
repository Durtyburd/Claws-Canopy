using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoDisplay : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandleSteamIdUpdated))]
    private ulong steamId;
    
    [SerializeField] private GameObject displayGO;
    [SerializeField] RawImage rawImage = null;

    protected Callback<AvatarImageLoaded_t> avatarImageLoaded;
    
    #region Server

    public void SetSteamId(ulong steamId)
    {
        this.steamId = steamId;
    }

    #endregion

    #region Client

    public override void OnStartClient()
    {
        base.OnStartClient();
        PlayerList.Instance.RegisterPlayer(displayGO.transform);
        avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        if (callback.m_steamID.m_SteamID != this.steamId)
        {
            return;
        }
        
        rawImage.texture = GetSteamImageAsTexture2D(callback.m_iImage);
    }
    
    private void HandleSteamIdUpdated(ulong oldId, ulong newId)
    {
        CSteamID cSteamId = new CSteamID(newId);
        // nameText.text = SteamFriends.GetFriendPersonaName(cSteamId);

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
        return texture;
    }
    
    #endregion
}