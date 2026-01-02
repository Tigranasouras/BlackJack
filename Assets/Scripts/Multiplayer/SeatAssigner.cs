using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using System.Collections;

public class SeatAssigner : MonoBehaviour
{
    [SerializeField] MultiplayerGameManager game;   // your running table
    [SerializeField] UnityEngine.UI.Image[] avatarSlots; // 4 images (optional)

    // --- call this from SeatAssignerBootstrap ---
    public void AssignFromLobby(CSteamID lobbyId)
    {
        StopAllCoroutines();
        StartCoroutine(CoAssign(lobbyId));
    }

    private IEnumerator CoAssign(CSteamID lobbyId)
    {
        // Safety guards
        if (!SteamManager.Initialized || !lobbyId.IsValid())
        {
            LocalSoloFallback();
            yield break;
        }

        //ensure table exists
        game.BuildEmptyTable(game.seatUIs.Count);

        //warmup: wait a moment until members are visible
        float timeout = 3f;
        int count = SteamMatchmaking.GetNumLobbyMembers(lobbyId);

        while (count == 0 && timeout > 0f)
        {
            // request data (async) and give Steam a frame to process
            SteamMatchmaking.RequestLobbyData(lobbyId);
            yield return null;
            timeout -= Time.deltaTime;
            count = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        }

        if (count == 0)
        {
            Debug.LogWarning("[SeatAssigner] Lobby Empty after warmup, using solo fallback.");
            LocalSoloFallback();
            yield break;
        }

        // Host controls whether empty seats become bots.
        bool fillBots = SteamMatchmaking.GetLobbyData(lobbyId, "fillBots") == "1";

        // Prefer a stable seat map stored in lobby data (rejoin-friendly).
        ulong[] seatSteamIds = new ulong[game.seats.Count];
        bool hasSeatMap = false;
        for (int i = 0; i < seatSteamIds.Length; i++)
        {
            string v = SteamMatchmaking.GetLobbyData(lobbyId, $"seat{i}");
            if (ulong.TryParse(v, out var sid) && sid != 0)
            {
                seatSteamIds[i] = sid;
                hasSeatMap = true;
            }
        }

        // Fallback: if no seat map exists yet, use Steam's member ordering.
        if (!hasSeatMap)
        {
            for (int i = 0; i < game.seats.Count; i++)
            {
                if (i < count)
                    seatSteamIds[i] = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i).m_SteamID;
            }
        }

        for (int seat = 0; seat < game.seats.Count; seat++)
        {
            ulong sid = seatSteamIds[seat];
            if (sid != 0)
            {
                var id = new CSteamID(sid);
                string name = SteamFriends.GetFriendPersonaName(id);
                game.SetSeatOwner(seat, sid, name, isBot: false, isActive: true);

                if (avatarSlots != null && seat < avatarSlots.Length && avatarSlots[seat])
                {
                    var sp = SteamImageUtils.GetAvatarSprite(id, true);
                    avatarSlots[seat].sprite = sp;
                    var c = avatarSlots[seat].color; c.a = sp ? 1f : 0.4f; avatarSlots[seat].color = c;
                }
            }
            else
            {
                if (fillBots)
                    game.SetSeatOwner(seat, 0UL, $"Bot{seat + 1}", isBot: true, isActive: true);
                else
                    game.SetSeatOwner(seat, 0UL, "", isBot: true, isActive: false);
            }
        }

        var my = SteamUser.GetSteamID().m_SteamID;
        int localSeat = -1;
        for (int i = 0; i < game.seats.Count; i++)
            if (game.seats[i].ownerSteamId == my && !game.seats[i].player.isBot) { localSeat = i; break; }
        game.BeginBettingPhase(); //enable only the human seat's wager buttons
        if (game.sharedControls) game.sharedControls.BindToSeat(game, localSeat, my);
        if (game.sharedControls) game.sharedControls.SetBettingEnabled(localSeat >= 0); //turn on wagers
    }


    public void LocalSoloFallback()
    {
        game.BuildEmptyTable(game.seatUIs.Count);

        var me = SteamManager.Initialized ? SteamUser.GetSteamID().m_SteamID : 1UL;
        game.SetSeatOwner(0, me, SteamManager.Initialized ? SteamFriends.GetPersonaName() : "You", isBot: false, isActive: true);

        for (int i = 1; i < game.seats.Count; i++)
            game.SetSeatOwner(i, 0UL, $"Bot{i + 1}", isBot: true, isActive: true);

        game.BeginBettingPhase();
    }
}