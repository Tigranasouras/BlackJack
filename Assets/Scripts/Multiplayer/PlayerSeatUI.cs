using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks; //for steamUser.GetSteamID()

public class PlayerSeatUI : MonoBehaviour
{
    //Seat
    public int seatIndex;
    public ulong ownerSteamId;
    public bool isBot;

    //Buttons
    public Button bet1, bet5, bet10, betAllIn;
    public Button hitBtn, standBtn, doubleBtn, splitBtn;

    //Labels
    public TextMeshProUGUI cashText;
    public TextMeshProUGUI wagerText;

    MultiplayerGameManager mgr;

    public void Init(MultiplayerGameManager manager, int seat, ulong steamID, bool bot)
    {
        mgr = manager;
        seatIndex = seat;
        ownerSteamId = steamID;
        isBot = bot;

        //Wager Buttons
        bet1.onClick.AddListener(() => mgr.RequestWager(seatIndex, 1, ownerSteamId));
        bet5.onClick.AddListener(() => mgr.RequestWager(seatIndex, 5, ownerSteamId));
        bet10.onClick.AddListener(() => mgr.RequestWager(seatIndex, 10, ownerSteamId));
        betAllIn.onClick.AddListener(() => mgr.RequestWager(seatIndex, -1, ownerSteamId)); // -1 = all in

        //Action buttons
        hitBtn.onClick.AddListener(() => mgr.RequestHit(seatIndex,ownerSteamId));
        standBtn.onClick.AddListener(() => mgr.RequestStand(seatIndex, ownerSteamId));
        doubleBtn.onClick.AddListener(() => mgr.RequestDouble(seatIndex, ownerSteamId));
        splitBtn.onClick.AddListener(() => mgr.RequestSplit(seatIndex, ownerSteamId));

    }

    public void SetInteractable (bool enabled)
    {
        //Action buttons (during turn)
        hitBtn.interactable = enabled;
        standBtn.interactable = enabled;
        doubleBtn.interactable = enabled;
        splitBtn.interactable = enabled;
    }

    public void SetBettingEnabled(bool enabled)
    {
        bet1.interactable = enabled;
        bet5.interactable = enabled;
        bet10.interactable = enabled;
        betAllIn.interactable = enabled;
    }

    public void UpdateMoneyUI(int cash, int wager)
    {
        if (cashText) cashText.text = $"${cash:N0}";
        if (wagerText) wagerText.text = $"Bet: ${wager:N0}";
    }

}
