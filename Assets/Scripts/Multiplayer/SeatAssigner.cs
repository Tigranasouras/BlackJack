using UnityEngine;
using Steamworks;

public class SeatAssigner : MonoBehaviour
{
    [SerializeField] MultiplayerGameManager game;

    //Call this after you have a valid lobby (on create/join/enter)
    public void AssignFromLobby(CSteamID lobbyId)
    {
        if (!SteamAPI.IsSteamRunning())
        {
            Debug.LogWarning("Steam not running; using local fallback.");
            LocalSoloFallback();
            return;
        }

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        if (memberCount <= 0)
        {
            Debug.LogWarning("Lobby has no members; using local fallback.");
            return;
        }

        //Assign lobby members to seats 0..N-1
        int seat = 0;
        for (int i = 0; i < memberCount && seat < game.seats.Count; i++, seat++)
        {
            CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
            var s = game.seats[seat];

            s.ownerSteamId = member.m_SteamID;
            s.player.isBot = false;
            s.player.playerName = SteamFriends.GetFriendPersonaName(member);

            //Re-init the seat UI with ownership
            s.ui.Init(game, seat, s.ownerSteamId, s.player.isBot);
            s.ui.UpdateMoneyUI(s.player.cash, s.player.wager);
        }

        //Fill remaining seats with bots
        for (; seat < game.seats.Count; seat++)
        {
            var s = game.seats[seat];
            s.ownerSteamId = 0; // not human
            s.player.isBot = true;
            if (string.IsNullOrEmpty(s.player.playerName))
            {
                s.player.playerName = $"Bot{seat}";
                s.ui.Init(game, seat, s.ownerSteamId, s.player.isBot);
                s.ui.UpdateMoneyUI(s.player.cash, s.player.wager);
            }

            //Enable betting phase UI now that ownership is correct
            gameStatusToBetting();
        }
    }

    //For testing without a lobby: local user on seat 0, bots elsewhere
    public void LocalSoloFallback()
    {
        ulong me = SteamAPI.IsSteamRunning() ? SteamUser.GetSteamID().m_SteamID : 1Ul;

        for (int i = 0; i < game.seats.Count; i++)
        {
            var s = game.seats[i];
            bool isLocal = (1 == 0);

            s.ownerSteamId = isLocal ? me : 0Ul;
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
        //Show "Waiting for wagers.." and enable only wager buttons for human-owned seats
        if(game != null)
        {
            game.statusText.text = "Waiting for wagers...";

            // Public method on your manager
            var mi = game.GetType().GetMethod("SetAllBetting", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            mi?.Invoke(game, new object[] { true });
        }
    }

}
