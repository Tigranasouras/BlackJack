using UnityEngine;
using Steamworks;
using System.Collections;

public class SeatAssignerBootstrap : MonoBehaviour
{
    [SerializeField] SeatAssigner seatAssigner;
    [SerializeField] MultiplayerGameManager game;

    //Static guard so we never double-register
    private static Callback<LobbyEnter_t> s_cbLobbyEnter;
    private static bool s_hooked;
    private static bool s_gotLobbyEnter;
    private static CSteamID s_lastEnteredLobby = CSteamID.Nil;


    private void Awake()
    {
        //ensure we only hook once per process
        if (!s_hooked && SteamManager.Initialized)
        {
            s_cbLobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            s_hooked = true;

        }
    }

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            StartCoroutine(WaitForSteamThenAssign());
            return;
        }
        StartCoroutine(AssignSeatsFlow());
    }

    private void OnLobbyEnter(LobbyEnter_t cb)
    {
        s_gotLobbyEnter = true;
        s_lastEnteredLobby = new CSteamID(cb.m_ulSteamIDLobby);
        Debug.Log($"[SeatAssignerBootstrap] OnLobbyEnter -> {s_lastEnteredLobby.m_SteamID} (resp: {(EChatRoomEnterResponse)cb.m_EChatRoomEnterResponse})");

        // ignore bogus 0 callbacks
        if (!s_lastEnteredLobby.IsValid()) return;

        var bridge = LobbyBridge.Instance;
        if (bridge != null) bridge.SetLobby(s_lastEnteredLobby);

    }

    private IEnumerator WaitForSteamThenAssign()
    {
        while (!SteamManager.Initialized) yield return null;
        if (!s_hooked) { s_cbLobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter); s_hooked = true; }
        yield return AssignSeatsFlow();
    }

    private IEnumerator AssignSeatsFlow()
    {
        var bridge = LobbyBridge.Instance;
        var target = (bridge != null && bridge.HasLobby) ? bridge.LobbyId : CSteamID.Nil;

        if (!target.IsValid())
        {
            // No lobby → solo fallback
            seatAssigner.LocalSoloFallback();
            game.statusText.text = "Solo test – waiting for wagers..";
            yield break;
        }

        // If not already in, join
        bool alreadyMember = false;
        try { alreadyMember = SteamMatchmaking.GetNumLobbyMembers(target) > 0; } catch { alreadyMember = false; }

        if (!alreadyMember)
        {
            Debug.Log($"[SeatAssignerBootstrap] Joining lobby {target.m_SteamID} …");
            s_gotLobbyEnter = false;
            s_lastEnteredLobby = CSteamID.Nil;
            SteamMatchmaking.JoinLobby(target);
        }

        // Wait for the real enter OR members>0 (ignore bogus enter with 0)
        const float timeout = 8f;
        float t = 0f;
        while (t < timeout)
        {
            var use = s_lastEnteredLobby.IsValid() ? s_lastEnteredLobby : target;
            SteamMatchmaking.RequestLobbyData(use);

            bool memberNow = false;
            try
            {
                int count = SteamMatchmaking.GetNumLobbyMembers(use);
                memberNow = (count > 0);
                if (memberNow) Debug.Log($"[SeatAssignerBootstrap] Members now: {count} (using {use.m_SteamID})");
            }
            catch { /* ignore */ }

            if ((s_gotLobbyEnter && s_lastEnteredLobby.IsValid()) || memberNow) break;

            t += Time.deltaTime;
            yield return null;
        }

        var finalLobby = s_lastEnteredLobby.IsValid() ? s_lastEnteredLobby : target;

        // a couple frames for member cache to settle
        yield return null; yield return null;

        // Assign seats & enable betting
        seatAssigner.AssignFromLobby(finalLobby);
        game.statusText.text = "Waiting for wagers..";
        var mi = typeof(MultiplayerGameManager).GetMethod(
            "SetAllBetting",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public
        );
        mi?.Invoke(game, new object[] { true });
    }
}