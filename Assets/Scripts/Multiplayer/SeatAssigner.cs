using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using System.Collections;

public class SeatAssigner : MonoBehaviour
{
    [SerializeField] MultiplayerGameManager game;

    // Call this
    public void AssignFromLobby(CSteamID lobbyId)
    {
        StartCoroutine(AssignFromLobbyCo(lobbyId));
    }

    private IEnumerator AssignFromLobbyCo(CSteamID lobbyId)
    {
        if (!SteamAPI.IsSteamRunning())
        {
            Debug.LogWarning("[SeatAssigner] Steam not running; using local fallback.");
            LocalSoloFallback();
            yield break;
        }

        // Wait for members to be visible
        const float timeout = 6f;
        float t = 0f;
        int memberCount = 0;

        while (t < timeout)
        {
            SteamMatchmaking.RequestLobbyData(lobbyId);

            try { memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId); }
            catch { memberCount = 0; }

            if (memberCount > 0) break;

            t += Time.deltaTime;
            yield return null;
        }

        if (memberCount <= 0)
        {
            Debug.LogWarning("[SeatAssigner] Lobby has 0 members (timed out); using local fallback.");
            LocalSoloFallback();
            yield break;
        }

        var owner = SteamMatchmaking.GetLobbyOwner(lobbyId);
        var me = SteamUser.GetSteamID();
        Debug.Log($"[SeatAssigner] lobby:{lobbyId.m_SteamID} owner:{owner.m_SteamID} me:{me.m_SteamID} members:{memberCount}");


        // Assign lobby members to seats
        int seat = 0;
        for (int i = 0; i < memberCount && seat < game.seats.Count; i++, seat++)
        {
            CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
            var s = game.seats[seat];

            s.ownerSteamId = member.m_SteamID;
            s.player.isBot = false;
            s.player.playerName = SteamFriends.GetFriendPersonaName(member);

            s.ui.Init(game, seat, s.ownerSteamId, s.player.isBot);
            s.ui.UpdateMoneyUI(s.player.cash, s.player.wager);
        }

        // Fill remaining seats with bots
        for (; seat < game.seats.Count; seat++)
        {
            var s = game.seats[seat];
            s.ownerSteamId = 0UL;
            s.player.isBot = true;
            if (string.IsNullOrEmpty(s.player.playerName))
                s.player.playerName = $"Bot{seat}";
            s.ui.Init(game, seat, s.ownerSteamId, s.player.isBot);
            s.ui.UpdateMoneyUI(s.player.cash, s.player.wager);
        }

        gameStatusToBetting();
    }

    // For testing without a lobby: local user on seat 0, bots elsewhere
    public void LocalSoloFallback()
    {
        ulong me = SteamAPI.IsSteamRunning() ? SteamUser.GetSteamID().m_SteamID : 1UL;

        for (int i = 0; i < game.seats.Count; i++)
        {
            var s = game.seats[i];
            bool isLocal = (i == 0);

            s.ownerSteamId = isLocal ? me : 0UL;
            s.player.isBot = !isLocal;

            if (isLocal && string.IsNullOrEmpty(s.player.playerName))
                s.player.playerName = SteamFriends.GetPersonaName();
            if (!isLocal && string.IsNullOrEmpty(s.player.playerName))
                s.player.playerName = $"Bot{i}";

            s.ui.Init(game, i, s.ownerSteamId, s.player.isBot);
            s.ui.UpdateMoneyUI(s.player.cash, s.player.wager);
        }

        gameStatusToBetting();
    }

    private void gameStatusToBetting()
    {
        if (game != null)
        {
            game.statusText.text = "Waiting for wagers...";
            var mi = game.GetType().GetMethod("SetAllBetting",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            mi?.Invoke(game, new object[] { true });
        }
    }
}