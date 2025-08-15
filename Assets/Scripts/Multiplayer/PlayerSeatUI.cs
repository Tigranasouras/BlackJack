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

        //if this seat doesn't belong to human, don't wire wager buttons
        bool isHuman = ownerSteamId != 0 && !isBot;

    }


    public void UpdateMoneyUI(int cash, int wager)
    {
        if (cashText) cashText.text = $"${cash:N0}";
        if (wagerText) wagerText.text = $"Bet: ${wager:N0}";
    }

}
