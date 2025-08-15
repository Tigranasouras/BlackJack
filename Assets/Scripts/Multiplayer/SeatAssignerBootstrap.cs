using UnityEngine;
using Steamworks;
using System.Collections;

public class SeatAssignerBootstrap : MonoBehaviour
{
    [SerializeField] SeatAssigner seatAssigner;
    [SerializeField] MultiplayerGameManager game;

    IEnumerator Start()
    {
        while (!SteamManager.Initialized) yield return null;

        var bridge = LobbyBridge.Instance;

        if (bridge && bridge.HasLobby && bridge.Entered)
        {
            float timeout = 5f;
            while (bridge.GetNumLobbyMembers() == 0 && timeout > 0f)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            Debug.Log($"[SeatAssignerBootstrap] Lobby {bridge.LobbyId} members={bridge.GetNumLobbyMembers()}");
            seatAssigner.AssignFromLobby(bridge.LobbyId);

            if (game) game.statusText.text = "Waiting for wagers..";
            var mi = typeof(MultiplayerGameManager).GetMethod("SetAllBetting",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            mi?.Invoke(game, new object[] { true });
        }
        else
        {
            seatAssigner.LocalSoloFallback();
            if (game) game.statusText.text = "Solo test – waiting for wagers..";
        }
    }
}