using UnityEngine;
using Steamworks;

public class LobbyBridge : MonoBehaviour
{
    public static LobbyBridge Instance { get; private set; }

    public CSteamID LobbyId { get; private set; } = CSteamID.Nil;
    public bool HasLobby => LobbyId.IsValid();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetLobby(CSteamID id) => LobbyId = id;
    public void Clear() => LobbyId = CSteamID.Nil;
}
