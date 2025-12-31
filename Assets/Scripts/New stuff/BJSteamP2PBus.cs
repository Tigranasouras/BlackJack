using System;
using System.Text;
using Steamworks;
using UnityEngine;

/// <summary>
/// Simple Steam P2P packet bus using SteamNetworking (classic P2P).
/// Why: Steamworks.NET versions differ on SteamNetworkingMessages APIs (ESteamNetworkingSend / SteamNetworkingMessage_t fields).
/// This bus avoids those newer APIs and uses SendP2PPacket/ReadP2PPacket which are stable across Steamworks.NET versions.
///
/// Contract:
/// - Host (lobby owner) receives client action messages.
/// - Host broadcasts authoritative STATE snapshots to all lobby members.
/// </summary>
public class BJSteamP2PBus : MonoBehaviour
{
    public static BJSteamP2PBus Instance { get; private set; }

    /// <summary> senderSteamId (ulong), json payload </summary>
    public event Action<ulong, string> OnJsonMessage;

    [Tooltip("Steam P2P channel used for all Blackjack messages.")]
    public int channel = 0;

    private Callback<P2PSessionRequest_t> _p2pSessionRequest;
    private Callback<P2PSessionConnectFail_t> _p2pConnectFail;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Accept sessions automatically when requested
        _p2pSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
        _p2pConnectFail = Callback<P2PSessionConnectFail_t>.Create(OnP2PConnectFail);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnP2PSessionRequest(P2PSessionRequest_t req)
    {
        // Required for P2P packets to flow
        SteamNetworking.AcceptP2PSessionWithUser(req.m_steamIDRemote);
    }

    private void OnP2PConnectFail(P2PSessionConnectFail_t fail)
    {
        Debug.LogWarning($"[BJSteamP2PBus] P2P connect fail to {fail.m_steamIDRemote.m_SteamID}, error={fail.m_eP2PSessionError}");
    }

    private void Update()
    {
        // Poll incoming packets
        uint size;
        while (SteamNetworking.IsP2PPacketAvailable(out size, channel))
        {
            byte[] buffer = new byte[size];
            uint bytesRead;
            CSteamID remote;
            if (SteamNetworking.ReadP2PPacket(buffer, size, out bytesRead, out remote, channel))
            {
                string json = Encoding.UTF8.GetString(buffer, 0, (int)bytesRead);
                OnJsonMessage?.Invoke(remote.m_SteamID, json);
            }
            else
            {
                // If read fails, break to avoid tight loop
                break;
            }
        }
    }

    public void SendToUser(ulong steamId, string json)
    {
        if (!SteamManager.Initialized) return;
        var bytes = Encoding.UTF8.GetBytes(json);

        // EP2PSendReliable is fine here (small messages). You can switch to Unreliable for frequent STATE updates if desired.
        SteamNetworking.SendP2PPacket(new CSteamID(steamId), bytes, (uint)bytes.Length, EP2PSend.k_EP2PSendReliable, channel);
    }

    public void BroadcastToLobby(CSteamID lobbyId, string json)
    {
        if (!SteamManager.Initialized) return;

        int count = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        for (int i = 0; i < count; i++)
        {
            CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
            SendToUser(member.m_SteamID, json);
        }
    }
}
