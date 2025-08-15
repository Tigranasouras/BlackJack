using UnityEngine;
using Steamworks;
using System.Collections;

public class SeatAssignerBootstrap : MonoBehaviour
{
    [SerializeField] SeatAssigner seatAssigner;
    [SerializeField] MultiplayerGameManager game;

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            StartCoroutine(WaitForSteamThenAssign());
            return;
        }
        StartCoroutine(AssignSeatsFlow());  
    }

    private System.Collections.IEnumerator WaitForSteamThenAssign()
    {
        while (!SteamManager.Initialized) yield return null;
        yield return AssignSeatsFlow();
    }

    private System.Collections.IEnumerator AssignSeatsFlow()
    {
        var bridge = LobbyBridge.Instance;

        // If we came from the lobby scene, wait briefly until Steam reports members.
        if (bridge != null && bridge.HasLobby)
        {
            // Short warm-up so GetNumLobbyMembers > 0 reliably
            float timeout = 5f; // seconds (tweak if needed)
            int members = SteamMatchmaking.GetNumLobbyMembers(bridge.LobbyId);
            while (members == 0 && timeout > 0f)
            {
                yield return null;               // wait a frame
                timeout -= Time.deltaTime;
                members = SteamMatchmaking.GetNumLobbyMembers(bridge.LobbyId);
            }

            Debug.Log($"[SeatAssignerBootstrap] Lobby {bridge.LobbyId} members={members}");

            seatAssigner.AssignFromLobby(bridge.LobbyId);
            if (game) game.statusText.text = "Waiting for wagers..";

            // enable betting for human-owned seats
            var mi = typeof(MultiplayerGameManager).GetMethod(
                "SetAllBetting",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public
            );
            mi?.Invoke(game, new object[] { true });
        }
        else
        {
            // Launched Multiplayer scene directly (no lobby)
            seatAssigner.LocalSoloFallback();
            if (game) game.statusText.text = "Solo test – waiting for wagers..";
        }
    }
}