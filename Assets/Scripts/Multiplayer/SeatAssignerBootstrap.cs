using UnityEngine;
using Steamworks;

public class SeatAssignerBootstrap : MonoBehaviour
{
    [SerializeField] SeatAssigner seatAssigner;

    private void Start()
    {
        var bridge = LobbyBridge.Instance;
        if(bridge != null && bridge.HasLobby)
        {
            seatAssigner.AssignFromLobby(bridge.LobbyId);
        }
        else
        {
            //No lobby (e.g., Launched directly) -> local solo fallback
            seatAssigner.LocalSoloFallback();
        }
    }
}
