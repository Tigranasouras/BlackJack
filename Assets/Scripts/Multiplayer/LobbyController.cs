using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Steamworks;
using System.Collections.Generic;
using System.Collections;

public class LobbyController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text headerText;
    public Button startButton;
    public Button exitButton;
    public Button inviteButton;
    public LobbySeatUI[] seatRows; // 4 rows; each has name, avatar, invite/leave if you want

    [Header("Config")]
    public string gameSceneName = "MultiPlayer";
    public int maxMembers = 4;
    public ELobbyType lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;

    // callbacks
    Callback<LobbyMatchList_t> cbMatchList;
    Callback<LobbyCreated_t> cbCreated;
    Callback<LobbyEnter_t> cbEnter;
    Callback<LobbyChatUpdate_t> cbChat;
    Callback<GameLobbyJoinRequested_t> cbJoinReq;
    Callback<LobbyDataUpdate_t> cbData;

    CSteamID current = CSteamID.Nil;
    LobbyBridge bridge;

    void Awake()
    {
        if (!SteamManager.Initialized) { Debug.LogError("Steam not initialized"); return; }

        bridge = LobbyBridge.Instance ?? new GameObject("LobbyBridge").AddComponent<LobbyBridge>();
        WireUI();

        // register callbacks
        cbMatchList = Callback<LobbyMatchList_t>.Create(OnMatchList);
        cbCreated = Callback<LobbyCreated_t>.Create(OnCreated);
        cbEnter = Callback<LobbyEnter_t>.Create(OnEnter);
        cbChat = Callback<LobbyChatUpdate_t>.Create(_ => RefreshMembers());
        cbJoinReq = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        cbData = Callback<LobbyDataUpdate_t>.Create(_ => RefreshMembers());
    }

    void Start()
    {
        headerText?.SetText("Looking for lobby...");
        SearchOrCreate();
    }

    void WireUI()
    {
        if (startButton) startButton.onClick.AddListener(OnStartGame);
        if (exitButton) exitButton.onClick.AddListener(LeaveAndBackToMenu);
        if (inviteButton) inviteButton.onClick.AddListener(OpenInviteOverlay);
        SetStartEnabled(false);
        ClearSeatUI();
    }

    void SearchOrCreate()
    {
        // You can add filters here:
        // SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(maxMembers);
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(5);
        SteamMatchmaking.RequestLobbyList();
    }

    void OnMatchList(LobbyMatchList_t cb)
    {
        int n = (int)cb.m_nLobbiesMatching;
        if (n > 0)
        {
            // Join the first one (nearest by default). You can score them.
            var id = SteamMatchmaking.GetLobbyByIndex(0);
            SteamMatchmaking.JoinLobby(id);
            headerText?.SetText("Joining lobby...");
        }
        else
        {
            // Create
            SteamMatchmaking.CreateLobby(lobbyType, maxMembers);
            headerText?.SetText("Creating lobby...");
        }
    }

    void OnCreated(LobbyCreated_t cb)
    {
        if (cb.m_eResult != EResult.k_EResultOK)
        {
            headerText?.SetText($"Lobby create failed: {cb.m_eResult}");
            return;
        }

        current = new CSteamID(cb.m_ulSteamIDLobby);
        bridge.SetLobby(current);
        SteamMatchmaking.SetLobbyJoinable(current, true);
        SteamMatchmaking.SetLobbyData(current, "name", "Dealer Advantage");
        SteamMatchmaking.SetLobbyData(current, "state", "lobby");
        headerText?.SetText("Lobby created");
        // Owner will also get OnEnter immediately after
    }

    void OnEnter(LobbyEnter_t cb)
    {
        current = new CSteamID(cb.m_ulSteamIDLobby);
        bridge.OnEntered(current); // mark as “we’re in”
        headerText?.SetText("In lobby");
        RefreshMembers();
        SetStartEnabled(IsOwner());
    }

    void OnJoinRequested(GameLobbyJoinRequested_t cb)
    {
        // When someone clicks a friend invite -> this will fire
        SteamMatchmaking.JoinLobby(cb.m_steamIDLobby);
    }

    void RefreshMembers()
    {
        if (!current.IsValid()) return;

        int count = SteamMatchmaking.GetNumLobbyMembers(current);
        var me = SteamUser.GetSteamID();

        for (int i = 0; i < seatRows.Length; i++)
        {
            if (i < count)
            {
                var id = SteamMatchmaking.GetLobbyMemberByIndex(current, i);
                string name = SteamFriends.GetFriendPersonaName(id);
                var sprite = SteamImageUtils.GetAvatarSprite(id, true);
                bool isLocal = id == me;
                seatRows[i].SetOccupied(name, sprite, isLocal);
            }
            else seatRows[i].SetEmpty();
        }

        SetStartEnabled(IsOwner());
    }

    bool IsOwner()
    {
        if (!current.IsValid()) return false;
        return SteamMatchmaking.GetLobbyOwner(current) == SteamUser.GetSteamID();
    }

    void SetStartEnabled(bool on) { if (startButton) startButton.interactable = on; }
    void ClearSeatUI() { foreach (var r in seatRows) if (r) r.SetEmpty(); }

    void OpenInviteOverlay()
    {
        if (current.IsValid())
            SteamFriends.ActivateGameOverlayInviteDialog(current);
    }

    void OnStartGame()
    {
        if (!IsOwner()) return;
        SteamMatchmaking.SetLobbyData(current, "state", "starting");
        SceneManager.LoadScene(gameSceneName);
    }

    void LeaveAndBackToMenu()
    {
        if (current.IsValid()) SteamMatchmaking.LeaveLobby(current);
        bridge.Clear();
        SceneManager.LoadScene("MainMenu");
    }
}
