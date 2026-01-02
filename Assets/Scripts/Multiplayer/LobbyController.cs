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
    public LobbySeatUI[] seatRows;

    [Header("Config")]
    public string gameSceneName = "MultiPlayer";
    public int maxMembers = 4;
    public ELobbyType lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;

    [Header("Bots (Optional)")]
    [Tooltip("If true, the host will fill empty seats with bots when the game starts.")]
    public bool fillEmptySeatsWithBots = true;

    [Tooltip("Optional UI toggle (only the host can change this).")]
    public Toggle fillBotsToggle;

    // Steam callbacks
    private Callback<LobbyMatchList_t> cbMatchList;
    private Callback<LobbyCreated_t> cbCreated;
    private Callback<LobbyEnter_t> cbEnter;
    private Callback<LobbyChatUpdate_t> cbChat;
    private Callback<GameLobbyJoinRequested_t> cbJoinReq;
    private Callback<LobbyDataUpdate_t> cbData;

    private CSteamID current = CSteamID.Nil;
    private LobbyBridge bridge;

    private void Awake()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam not initialized");
            return;
        }

        bridge = LobbyBridge.Instance ?? new GameObject("LobbyBridge").AddComponent<LobbyBridge>();
        WireUI();

        // Register callbacks (names now match the handlers below)
        cbMatchList = Callback<LobbyMatchList_t>.Create(OnMatchList);
        cbCreated = Callback<LobbyCreated_t>.Create(OnCreated);
        cbEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered);   // <-- fixed
        cbChat = Callback<LobbyChatUpdate_t>.Create(_ => RefreshMembers());
        cbJoinReq = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        cbData = Callback<LobbyDataUpdate_t>.Create(_ => RefreshMembers());
    }

    private void Start()
    {
        headerText?.SetText("Looking for lobby...");
        SearchOrCreate();
    }

    private void WireUI()
    {
        if (startButton) startButton.onClick.AddListener(OnStartGame);
        if (exitButton) exitButton.onClick.AddListener(LeaveAndBackToMenu);
        if (inviteButton) inviteButton.onClick.AddListener(OpenInviteOverlay);

        if (fillBotsToggle)
        {
            fillBotsToggle.isOn = fillEmptySeatsWithBots;
            fillBotsToggle.onValueChanged.AddListener(v =>
            {
                fillEmptySeatsWithBots = v;
                if (IsOwner()) WriteFillBotsSetting();
            });
        }

        SetStartEnabled(false);
        ClearSeatUI();
    }

    private void SearchOrCreate()
    {
        // You can add more filters here (slots available, distance, etc.)
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(5);
        SteamMatchmaking.RequestLobbyList();
    }

    private void OnMatchList(LobbyMatchList_t cb)
    {
        int n = (int)cb.m_nLobbiesMatching;
        if (n > 0)
        {
            var id = SteamMatchmaking.GetLobbyByIndex(0); // nearest by default
            SteamMatchmaking.JoinLobby(id);
            headerText?.SetText("Joining lobby...");
        }
        else
        {
            SteamMatchmaking.CreateLobby(lobbyType, maxMembers);
            headerText?.SetText("Creating lobby...");
        }
    }

    private void OnCreated(LobbyCreated_t cb)
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

        // Persist a stable seat mapping in lobby data so re-joins don't reshuffle seats.
        InitializeSeatMapAsOwner();
        WriteFillBotsSetting();

        headerText?.SetText("Lobby created");
        // Owner will also receive LobbyEnter shortly after
    }

    private void OnLobbyEntered(LobbyEnter_t cb)   // <-- name matches registration
    {
        current = new CSteamID(cb.m_ulSteamIDLobby);
        bridge.SetLobby(current);                  // <-- use 'current', not currentLobby
        bridge.MarkEntered();                      // tells your game scene you’re actually in
        headerText?.SetText("In lobby");

        // If this lobby is already starting (e.g. rejoin / late join), jump straight to game.
        string state = SteamMatchmaking.GetLobbyData(current, "state");
        if (state == "starting")
        {
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        if (IsOwner())
        {
            OwnerReconcileSeatMap();
            WriteFillBotsSetting();
        }

        RefreshMembers();
        SetStartEnabled(IsOwner());
    }

    private void OnJoinRequested(GameLobbyJoinRequested_t cb)
    {
        // When a friend clicks an invite
        SteamMatchmaking.JoinLobby(cb.m_steamIDLobby);
    }

    private void RefreshMembers()
    {
        if (!current.IsValid()) return;

        if (IsOwner())
            OwnerReconcileSeatMap();

        var me = SteamUser.GetSteamID();
        var owner = SteamMatchmaking.GetLobbyOwner(current);

        // Only host can change bot setting
        if (fillBotsToggle)
        {
            fillBotsToggle.gameObject.SetActive(IsOwner());
            fillBotsToggle.isOn = GetFillBotsSetting();
        }

        for (int i = 0; i < seatRows.Length; i++)
        {
            if (seatRows[i]?.inviteButton)
            {
                seatRows[i].inviteButton.onClick.RemoveAllListeners();
                seatRows[i].inviteButton.onClick.AddListener(() =>
                {
                    if (current.IsValid())
                        SteamFriends.ActivateGameOverlayInviteDialog(current);
                });
            }
            if (seatRows[i]?.leaveButton)
            {
                seatRows[i].leaveButton.onClick.RemoveAllListeners();
                seatRows[i].leaveButton.onClick.AddListener(LeaveAndBackToMenu);
            }

            ulong seatU64 = GetSeatSteamId(i);
            if (seatU64 != 0UL)
            {
                var id = new CSteamID(seatU64);
                string name = SteamFriends.GetFriendPersonaName(id);
                var avatar = SteamImageUtils.GetAvatarSprite(id, true);
                bool isLocal = id == me;
                seatRows[i].SetOccupied(name, avatar, isLocal);
            }
            else
            {
                seatRows[i].SetEmpty();
            }
        }

        SetStartEnabled(IsOwner());
    }

    private bool IsOwner()
    {
        if (!current.IsValid()) return false;
        return SteamMatchmaking.GetLobbyOwner(current) == SteamUser.GetSteamID();
    }

    private void SetStartEnabled(bool on)
    {
        if (startButton) startButton.interactable = on;
    }

    private void ClearSeatUI()
    {
        foreach (var r in seatRows) if (r) r.SetEmpty();
    }

    // -----------------------------
    // Stable seat mapping in lobby data (rejoin-friendly)
    // -----------------------------

    private const string SEAT_PREFIX = "seat_";
    private const string FILLBOTS_KEY = "fillBots";

    private string SeatKey(int i) => SEAT_PREFIX + i;

    private ulong GetSeatSteamId(int seatIndex)
    {
        string v = SteamMatchmaking.GetLobbyData(current, SeatKey(seatIndex));
        if (ulong.TryParse(v, out var u)) return u;
        return 0UL;
    }

    private void SetSeatSteamId(int seatIndex, ulong steamId)
    {
        SteamMatchmaking.SetLobbyData(current, SeatKey(seatIndex), steamId.ToString());
    }

    private void InitializeSeatMapAsOwner()
    {
        if (!IsOwner()) return;
        // Put current members into seats in order (owner will be present).
        int count = SteamMatchmaking.GetNumLobbyMembers(current);
        for (int i = 0; i < maxMembers; i++)
        {
            ulong id = 0UL;
            if (i < count)
                id = SteamMatchmaking.GetLobbyMemberByIndex(current, i).m_SteamID;
            SetSeatSteamId(i, id);
        }
    }

    private void OwnerReconcileSeatMap()
    {
        if (!IsOwner()) return;

        // Load existing map
        ulong[] map = new ulong[maxMembers];
        for (int i = 0; i < maxMembers; i++)
            map[i] = GetSeatSteamId(i);

        // Current lobby members
        int count = SteamMatchmaking.GetNumLobbyMembers(current);
        var members = new HashSet<ulong>();
        for (int i = 0; i < count; i++)
            members.Add(SteamMatchmaking.GetLobbyMemberByIndex(current, i).m_SteamID);

        // Remove leavers
        for (int i = 0; i < map.Length; i++)
            if (map[i] != 0UL && !members.Contains(map[i]))
                map[i] = 0UL;

        // Add joiners to first empty seat
        foreach (var id in members)
        {
            bool already = false;
            for (int i = 0; i < map.Length; i++)
                if (map[i] == id) { already = true; break; }
            if (already) continue;

            for (int i = 0; i < map.Length; i++)
            {
                if (map[i] == 0UL)
                {
                    map[i] = id;
                    break;
                }
            }
        }

        // Write back
        for (int i = 0; i < map.Length; i++)
            SetSeatSteamId(i, map[i]);
    }

    private void WriteFillBotsSetting()
    {
        if (!IsOwner()) return;
        SteamMatchmaking.SetLobbyData(current, FILLBOTS_KEY, fillEmptySeatsWithBots ? "1" : "0");
    }

    private bool GetFillBotsSetting()
    {
        string v = SteamMatchmaking.GetLobbyData(current, FILLBOTS_KEY);
        if (string.IsNullOrEmpty(v)) return fillEmptySeatsWithBots;
        return v == "1";
    }

    private void OpenInviteOverlay()
    {
        if (current.IsValid())
            SteamFriends.ActivateGameOverlayInviteDialog(current);
    }

    private void OnStartGame()
    {
        if (!IsOwner()) return;
        OwnerReconcileSeatMap();
        WriteFillBotsSetting();
        SteamMatchmaking.SetLobbyData(current, "state", "starting");
        SceneManager.LoadScene(gameSceneName);
    }

    private void LeaveAndBackToMenu()
    {
        if (current.IsValid()) SteamMatchmaking.LeaveLobby(current);
        bridge.Clear();
        SceneManager.LoadScene("MainMenu");
    }
}