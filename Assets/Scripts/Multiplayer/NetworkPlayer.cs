using UnityEngine;
using Unity.Netcode;

public class NetworkPlayer : NetworkBehaviour
{
    public NetworkVariable<int> playerScore = new NetworkVariable<int>();
    public NetworkVariable<int> cash = new NetworkVariable<int>(1000000);
    public NetworkVariable<int> wager = new NetworkVariable<int>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            //Enable local input buttons, camera, etc.
        }
    }

    [ServerRpc]
    public void RequestHitServerRPC()
    {
        //Deal Card from deck, update hand and score
    }

    [ServerRpc]
    public void PlaceWagerServerRpc(int amount)
    {
        if (cash.Value >= amount)
        {
            cash.Value -= amount;
            wager.Value += amount;
        }
    }

}
