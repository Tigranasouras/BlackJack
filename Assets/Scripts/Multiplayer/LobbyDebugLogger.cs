using UnityEngine;
using Steamworks;

public class LobbyDebugLogger : MonoBehaviour
{
    void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("[LobbyDebugLogger] Steam is not initialized");
            return;
        }

        //Log LobbyBridge ID
        if(LobbyBridge.Instance != null)
        {
            Debug.Log($"[LobbyDebugLogger] LobbyBridge Instance LobbyId: {LobbyBridge.Instance.LobbyId.m_SteamID}");
        }
        else
        {
            Debug.LogWarning("[LobbyDebugLogger] No LobbyBridge instance found.");
        }

        //Log all lobbies the player is in (usually 1)
        //This will help check if the player is actually in the LobbyBridge lobby
        var myId = SteamUser.GetSteamID();
        Debug.Log($"[LobbyDebugLogger ] My SteamID: {myId}");

        if (LobbyBridge.Instance != null && LobbyBridge.Instance.LobbyId.IsValid())
        {
            var lobbyId = LobbyBridge.Instance.LobbyId;
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            Debug.Log($"[LobbyDebugLogger ] Lobby {lobbyId} has {memberCount} members:");
        }
        else
        {
            Debug.LogWarning("[LobbyDebugLogger ] No valid Lobby ID to log members.");
        }

        }

    }