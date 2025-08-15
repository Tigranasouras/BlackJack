using UnityEngine;
using Steamworks;

public class LobbyBridge : MonoBehaviour
{
    public static LobbyBridge Instance { get; private set; }

    public CSteamID LobbyId { get; private set; } = CSteamID.Nil;
    public bool HasLobby => LobbyId.IsValid();
    public bool Entered { get; private set; } //we received LobbyEnter for it


    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    }

    public void SetLobby(CSteamID id) => LobbyId = id;
    public void OnEntered(CSteamID id) { LobbyId = id; Entered = id.IsValid(); }
    public void Clear() { LobbyId = CSteamID.Nil; Entered = false; }
}
