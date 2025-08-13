using UnityEngine;
using Steamworks;

public class SeatAssignerBootstrap : MonoBehaviour
{
    [SerializeField] SeatAssigner seatAssigner;
    [SerializeField] MultiplayerGameManager game;

    private void Start()
    {
        // Make sure Steam is initialized first (esp. when loading directly for tests)
        if (!SteamManager.Initialized)
        {
            StartCoroutine(WaitForSteamThenAssign());
            return;
        }
        AssignSeats();
    }

    private System.Collections.IEnumerator WaitForSteamThenAssign()
    {
        while (!SteamManager.Initialized) yield return null;
        AssignSeats();
    }

    private void AssignSeats()
    {
        var bridge = LobbyBridge.Instance;

        if (bridge != null && bridge.HasLobby)
        {
            // We came here from the lobby scene → use lobby members to own seats
            seatAssigner.AssignFromLobby(bridge.LobbyId);
            game.statusText.text = "Waiting for wagers..";
            // enable betting phase now that ownership is correct
            var mi = typeof(MultiplayerGameManager).GetMethod("SetAllBetting",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            mi?.Invoke(game, new object[] { true });
        }
        else
        {
            // Launched game scene directly → give seat 0 to local, bots for others
            seatAssigner.LocalSoloFallback();
            game.statusText.text = "Solo test – waiting for wagers..";
        }
    }
}
