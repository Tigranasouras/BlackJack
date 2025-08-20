using UnityEngine;
using Steamworks;
using UnityEngine.SceneManagement;

public class LobbyBridge : MonoBehaviour
{
    public static LobbyBridge Instance { get; private set; }

    public CSteamID LobbyId { get; private set; } = CSteamID.Nil;
    public bool HasLobby => LobbyId.IsValid();
    public bool Entered { get; private set; } //we received LobbyEnter for it

    [SerializeField] string lobbyScene = "Lobby";
    [SerializeField] string gameScene = "MultiPlayer";

    // Callbacks
    private Callback<GameLobbyJoinRequested_t> cbJoinReq;
    private Callback<LobbyEnter_t> cbEnter;
    private Callback<LobbyDataUpdate_t> cbData;


    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);

        if (!SteamManager.Initialized) return;
        cbJoinReq = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        cbEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        cbData = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
    }

    public void SetLobby(CSteamID id)
    {
        LobbyId = id;
        Entered = false; //Reset till we actually enter
    }
    public void Clear() { LobbyId = CSteamID.Nil; Entered = false; }

    private void OnJoinRequested(GameLobbyJoinRequested_t cb)
    {
        // Friend clicked invite while in any scene
        SteamMatchmaking.JoinLobby(cb.m_steamIDLobby);
        SetLobby(cb.m_steamIDLobby);
        // Optionally jump to lobby right away:
        if (SceneManager.GetActiveScene().name != lobbyScene)
            SceneManager.LoadScene(lobbyScene);
    }

    private void OnLobbyEntered(LobbyEnter_t cb)
    {
        var id = new CSteamID(cb.m_ulSteamIDLobby);
        SetLobby(id);
        Entered = true;

        // Ensure we are in the Lobby scene on both host and client
        if (SceneManager.GetActiveScene().name != lobbyScene)
            SceneManager.LoadScene(lobbyScene);
    }


    private void OnLobbyDataUpdated(LobbyDataUpdate_t cb)
    {
        if (!HasLobby || cb.m_ulSteamIDLobby != LobbyId.m_SteamID) return;

        string state = SteamMatchmaking.GetLobbyData(LobbyId, "state");
        if (state == "starting")
        {
            // Owner already loads the scene in LobbyController; this is for everyone else
            if (SceneManager.GetActiveScene().name != gameScene)
                SceneManager.LoadScene(gameScene);
        }
    }

    public void MarkEntered()
    {
        Entered = true; // call to LobbyEnter
    }

    public int GetNumLobbyMembers()
        => HasLobby ? SteamMatchmaking.GetNumLobbyMembers(LobbyId) : 0;

    
}
