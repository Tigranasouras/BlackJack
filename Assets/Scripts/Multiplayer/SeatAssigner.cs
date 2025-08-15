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
        StartCoroutine(CoAssignWithWarmup(lobbyId));
    }

    private IEnumerator CoAssignWithWarmup(CSteamID lobbyId)
    {
        // Safety guards
        if (!SteamManager.Initialized || !lobbyId.IsValid())
        {
            LocalSoloFallback();
            yield break;
        }

        // 1) brief warmup: let callbacks run & ask for lobby data a few frames
        const float maxWait = 2.5f; // total seconds to wait
        float elapsed = 0f;

        // (Optional) quick “hydration” period — RequestLobbyData and yield a few frames
        while (elapsed < maxWait)
        {
            // request data (async) and give Steam a frame to process
            SteamMatchmaking.RequestLobbyData(lobbyId);

            // if we already have members, break early
            int countNow = 0;
            try { countNow = SteamMatchmaking.GetNumLobbyMembers(lobbyId); } catch { }
            if (countNow > 0) break;

            // wait a frame
            yield return null;
            elapsed += Time.deltaTime;
        }

        // 2) one last check for members
        int memberCount = 0;
        try { memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId); } catch { }

        if (memberCount <= 0)
        {
            Debug.LogWarning("[SeatAssigner] Lobby still has 0 members after warmup; using local fallback.");
            LocalSoloFallback();
            yield break;
        }

        // 3) Assign members to seats 0..N-1
        var me = SteamUser.GetSteamID();
        for (int seat = 0; seat < game.seats.Count; seat++)
        {
            if (seat < memberCount)
            {
                var id = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, seat);
                var s = game.seats[seat];
                s.ownerSteamId = id.m_SteamID;
                s.player.isBot = false;
                s.player.playerName = SteamFriends.GetFriendPersonaName(id);
                s.ui.Init(game, seat, s.ownerSteamId, s.player.isBot);
                s.ui.UpdateMoneyUI(s.player.cash, s.player.wager);

                // Avatar (optional)
                if (avatarSlots != null && seat < avatarSlots.Length && avatarSlots[seat])
                {
                    var sp = SteamImageUtils.GetAvatarSprite(id, true);
                    avatarSlots[seat].sprite = sp;
                    var c = avatarSlots[seat].color; c.a = sp ? 1f : 0.4f; avatarSlots[seat].color = c;
                }
            }
            else
            {
                // Fill remaining seats with bots
                var s = game.seats[seat];
                s.ownerSteamId = 0;
                s.player.isBot = true;
                if (string.IsNullOrEmpty(s.player.playerName)) s.player.playerName = $"Bot{seat}";
                s.ui.Init(game, seat, s.ownerSteamId, s.player.isBot);
                s.ui.UpdateMoneyUI(s.player.cash, s.player.wager);
            }
        }

        // 4) enable betting for human-owned seats
        var mi = typeof(MultiplayerGameManager).GetMethod("SetAllBetting",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        mi?.Invoke(game, new object[] { true });

        game.statusText.text = "Waiting for wagers..";
    }

    // unchanged
    public void LocalSoloFallback()
    {
        var me = SteamManager.Initialized ? SteamUser.GetSteamID() : new CSteamID(1);
        for (int i = 0; i < game.seats.Count; i++)
        {
            bool isLocal = (i == 0);
            var s = game.seats[i];
            s.ownerSteamId = isLocal ? me.m_SteamID : 0;
            s.player.isBot = !isLocal;
            s.player.playerName = isLocal ? SteamFriends.GetPersonaName() : $"Bot{i}";
            s.ui.Init(game, i, s.ownerSteamId, s.player.isBot);
            s.ui.UpdateMoneyUI(s.player.cash, s.player.wager);
        }
        game.statusText.text = "Solo test – waiting for wagers..";
    }
}