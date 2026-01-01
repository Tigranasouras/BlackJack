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
        if (!SteamUtils.GetImageRGBA(imgId, rgba, rgba.Length)) return null;

        // Flip vertically (Steam often returns bottom-up)
        int width = (int)w;
        int height = (int)h;
        byte[] flipped = new byte[rgba.Length];

        int rowBytes = width * 4;
        for (int y = 0; y < height; y++)
        {
            int srcIndex = y * rowBytes;
            int dstIndex = (height - 1 - y) * rowBytes;
            System.Buffer.BlockCopy(rgba, srcIndex, flipped, dstIndex, rowBytes);
        }

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.LoadRawTextureData(flipped);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
    }
}
