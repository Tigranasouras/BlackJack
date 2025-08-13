using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using System.Collections;
using System.Collections.Generic;

public class GameLobbyVerifier : MonoBehaviour
{
    //UI
    public Image[] avatarSlots = new Image[4];
    public TMP_Text statusText;

    //Options
    public bool autoRefreshOnChatUpdate = true; // keep in sync as people join/leave
    public bool retryAvatarsUntilReady = true; // Avatars can arrive async from Steam

    private CSteamID lobbyId = CSteamID.Nil;
    private Callback<LobbyChatUpdate_t> cbLobbyChatUpdate;
    private bool initialized;

    private void Awake()
    {
        StartCoroutine(Bootstrap());
    }

    private IEnumerator Bootstrap()
    {
        //Wait for SteamManager + Steam Client to be really ready
        while (!IsSteamReallyReady())
            yield return null;

        //Wait a frame so DontDestroyOnLoad singeltones settle

        yield return new WaitForEndOfFrame();

        Init();
    }

    private bool IsSteamReallyReady()
    {
        //Guard all 3: SteamManager, SteamAPI client, and -optional- logged on 
        if (!SteamManager.Initialized) return false;
        if (!SteamAPI.IsSteamRunning()) return false;
        return true;
    }

    private void Init()
    {
        initialized = true;

        var bridge = LobbyBridge.Instance;
        if (bridge == null || !bridge.HasLobby)
        {
            SetStatus("No lobby found from LobbyBridge.");
            ClearAvatars();
            return;
        }


        lobbyId = bridge.LobbyId;
        SetStatus($"Lobby: {lobbyId.m_SteamID}");

        //optional: Auto refresh when members change
        if (autoRefreshOnChatUpdate)
            cbLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

        //first populate
        StartCoroutine(RefreshNow());
    }

    private void OnDestroy()
    {
        cbLobbyChatUpdate?.Dispose();
        cbLobbyChatUpdate = null;
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t _)
    {
        if (!lobbyId.IsValid()) return;
        StartCoroutine(RefreshNow());
    }

    private IEnumerator RefreshNow()
    {
        if (!initialized || !IsSteamReallyReady() || !lobbyId.IsValid())
        {
            SetStatus("Cannot refresh: Steam not ready or invalid lobby.");
            ClearAvatars();
            yield break;
        }

        //Satisfy net: never let a steam call crash the coroutine
        List<CSteamID> members = null;
        CSteamID me = CSteamID.Nil;
        try
        {
            me = SafeGetLocalSteamId();
            if (!me.IsValid())
            {
                SetStatus("Steam ID not ready yet.");
                ClearAvatars();
                yield break;
            }

            int count = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            members = new List<CSteamID>(count);
            for (int i = 0; i < count; i++)
                members.Add(SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GameLobbyVerifier] Steam call failed: {e.Message}");
            SetStatus("Steam not ready for lobby query.");
            ClearAvatars();
            yield break;
        }

        bool localFound = members.Contains(me);
        SetStatus(localFound
            ? $"Members: {members.Count} — OK"
            : " Local user not in LobbyBridge lobby (different lobby?).");

        for (int i = 0; i < avatarSlots.Length; i++)
        {
            if (i < members.Count)
                yield return SetAvatarFor(avatarSlots[i], members[i]);
            else
                SetEmpty(avatarSlots[i]);
        }
    }

    private CSteamID SafeGetLocalSteamId()
    {
        //Call only when IsSteamReallyRead() is true; still wrap in try
        try { return SteamUser.GetSteamID(); }
        catch { return CSteamID.Nil;  }
    }

    private IEnumerator SetAvatarFor(Image slot, CSteamID userId)
    {
        if (slot == null) yield break;

        // Try immediate
        var sprite = SteamImageUtils.GetAvatarSprite(userId, large: true);
        if (sprite == null && retryAvatarsUntilReady)
        {
            // Avatar can be fetched async by Steam; poll briefly
            const float timeout = 2.0f;
            float t = 0f;
            while (sprite == null && t < timeout)
            {
                yield return null;
                t += Time.deltaTime;
                sprite = SteamImageUtils.GetAvatarSprite(userId, large: true);
            }
        }

        if (sprite != null)
        {
            slot.sprite = sprite;
            var c = slot.color; c.a = 1f; slot.color = c;
        }
        else
        {
            // Fallback visual
            SetEmpty(slot);
        }
    }


    private void SetEmpty(Image slot)
    {
        if(slot == null) return;
        slot.sprite = null;
        var c = slot.color; c.a = 0.25f; slot.color = c;
    }

    private void ClearAvatars()
    {
        foreach (var img in avatarSlots) SetEmpty(img);
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        //also useful in console:
        Debug.Log($"[GameLobbyVerifier] {msg}");
    }







}
