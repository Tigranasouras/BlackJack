using UnityEngine;
using Steamworks;

public class SteamImageUtils : MonoBehaviour
{
    public static Sprite GetAvatarSprite(CSteamID id, bool large = true)
    {
        int imgId = large ? SteamFriends.GetLargeFriendAvatar(id) : SteamFriends.GetSmallFriendAvatar(id);
        if (imgId <= 0) return null;
        if (!SteamUtils.GetImageSize(imgId, out uint w, out uint h)) return null;


        byte[] rgba = new byte[w * h * 4];
        if(!SteamUtils.GetImageRGBA(imgId, rgba, (int)rgba.Length)) return null;

        var tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(rgba);
        tex.Apply();

        //Steam returns bottom-up; Unity expects top-down, but most avatars look fine either way.
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);

    }
}
