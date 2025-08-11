using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Steamworks;
using System.Collections.Generic;
using System.Collections;

public class LobbyController : MonoBehaviour
{
    //UI
    public TMP_Text headerText;    //optional
    public List<LobbySeatUI> seats; //Size = 4 in inspector
    public Button startButton;
    public Button exitButton;


    //Config
    public string gameSceneName = "MultiPlayer";
    public ELobbyType lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;
    public int maxMembers = 4;

    private CSteamID currentLobby = CSteamID.Nil;

    //Callbacks
    private Callback<LobbyCreated_t> cbLobbyCreated;
    private Callback<LobbyEnter_t> cbLobbyEntered;
    private Callback<LobbyChatUpdate_t> cbLobbyChatUpdate;
    private Callback<GameLobbyJoinRequested_t> cbGameLobbyJoinRequested;
    private Callback<LobbyDataUpdate_t> cbLobbyDataUpdate;

    private LobbyBridge bridge;
    

    private void Awake()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Steam not initialized yet, delaying lobby setup...");
            StartCoroutine(WaitForSteamInit());
            return;
        }
        EnsureBridge();
        InitLobbyController();
        
    }


    private IEnumerator WaitForSteamInit()
    {
        while (!SteamManager.Initialized)
            yield return null;

        InitLobbyController();
    }

    private void InitLobbyController()
    {
        if (!SteamAPI.IsSteamRunning())
        {
            Debug.LogError("Steam not running. Start Steam before running the Lobby.");
            return;
        }

        cbLobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        cbLobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        cbLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        cbGameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        cbLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);

        if (startButton) startButton.onClick.AddListener(OnStartClicked);
        if (exitButton) exitButton.onClick.AddListener(OnExitClicked);

        SetStartButtonInteractable(false);
        ClearSeatsUI();

        //Host Creates lobby if not already in one
        SteamMatchmaking.CreateLobby(lobbyType, maxMembers);
        if (headerText) headerText.text = "Creating Lobby...";
    }

    private void Update() => SteamAPI.RunCallbacks();

    //CallBacks
    private void OnLobbyCreated(LobbyCreated_t cb)
    {
        if(cb.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Lobby create faild: " + cb.m_eResult);
            if (headerText) headerText.text = "Lobby create failed.";
            return;
        }

        currentLobby = new CSteamID(cb.m_ulSteamIDLobby);
        bridge.SetLobby(currentLobby);
        //Set some data so invites show a nice name
        SteamMatchmaking.SetLobbyData(currentLobby, "name", "Dealer Advantage");
        SteamMatchmaking.SetLobbyJoinable(currentLobby, true);
    }

    private void OnLobbyEntered(LobbyEnter_t cb)
    {
        currentLobby = new CSteamID(cb.m_ulSteamIDLobby);
        bridge.SetLobby(currentLobby);
        if (headerText) headerText.text = "Dealer Advantage";

        RefreshSeatList();
        WireSeatButtons();
        SetStartButtonInteractable(IsLocalOwner());
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t cb)
    {
        if (currentLobby.IsValid())
            RefreshSeatList();
    }


    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t cb)
    {
    //When users click invite, friend end up here -> join their lobby
    SteamMatchmaking.JoinLobby(cb.m_steamIDLobby);
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t cb)
    {
        //could react to "State=start" etc. if you coordinate scene loads through lobby data
    }

    //UI Wiring

    private void WireSeatButtons()
    {
        //Invite on each row opens the overlay for THIS lobby
        foreach(var seat in seats)
        {
            if(seat == null) continue;
            if(seat.inviteButton)
            {
                seat.inviteButton.onClick.RemoveAllListeners();
                seat.inviteButton.onClick.AddListener(() =>
                {
                    if (currentLobby.IsValid())
                        SteamFriends.ActivateGameOverlayInviteDialog(currentLobby);
                });
            }
            if(seat.leaveButton)
            {
                seat.leaveButton.onClick.RemoveAllListeners();
                seat.leaveButton.onClick.AddListener(() =>
                {
                    if (currentLobby.IsValid())
                        SteamMatchmaking.LeaveLobby(currentLobby);
                    bridge.Clear();
                    ClearSeatsUI();
                    SetStartButtonInteractable(false);
                    if (headerText) headerText.text = "Left lobby.";
                });
            }
        }
    }


    private void SetStartButtonInteractable(bool enabled)
    {
        if (startButton) startButton.interactable = enabled;
    }

    private bool IsLocalOwner()
    {
        if (!currentLobby.IsValid()) return false;
        return SteamMatchmaking.GetLobbyOwner(currentLobby) == SteamUser.GetSteamID();
    }

    //Seat population
    private void RefreshSeatList()
    {
        if (!currentLobby.IsValid()) return;
        
        int count = SteamMatchmaking.GetNumLobbyMembers(currentLobby);
        var members = new List<CSteamID>(count);
        for (int i = 0; i < count; i++)
            members.Add(SteamMatchmaking.GetLobbyMemberByIndex(currentLobby, i));

        //Fill rows in order; empty the rest
        for(int i = 0; i < seats.Count; i++)
        {
            var seat = seats[i];
            if (seats == null) continue;

            if (i < members.Count)
            {
                var id = members[i];
                string name = SteamFriends.GetFriendPersonaName(id);
                var sprite = SteamImageUtils.GetAvatarSprite(id, true);
                bool isLocal = id == SteamUser.GetSteamID();
                seat.SetOccupied(name, sprite, isLocal);
            }
            else
            {
                seat.SetEmpty();
            }
        }

        //Only the owner can press Start
        SetStartButtonInteractable(IsLocalOwner());
    }

    private void ClearSeatsUI()
    {
        foreach(var s in seats) if (s) s.SetEmpty();
    }

    //Buttons

    private void OnStartClicked()
    {
        if (!IsLocalOwner()) return;

        //optional: mark lobby as "Starting" so late joiners know
        SteamMatchmaking.SetLobbyData(currentLobby, "State", "Starting");

        //Persist the lobby ID across scenes so your MultiplayerGameManager
        //Can assign seats based on these lobby members.
       // I think: DontDestroyOnLoad(gameObject);
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnExitClicked()
    {
        if (currentLobby.IsValid())
            SteamMatchmaking.LeaveLobby(currentLobby);
        bridge.Clear();
        Application.Quit(); //Not sure about this
    }

    private void EnsureBridge()
    {
        bridge = LobbyBridge.Instance;
        if (bridge == null)
        {
            var go = new GameObject("LobbyBridge");
            bridge = go.AddComponent<LobbyBridge>();
        }
    }

}
