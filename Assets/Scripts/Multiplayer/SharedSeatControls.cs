using UnityEngine;
using UnityEngine.UI;

public class SharedSeatControls : MonoBehaviour
{
    [Header("Buttons (one set in scene)")]

    public Button bet1, bet5, bet10, betAllIn;
    public Button hitBtn, standBtn, doubleBtn, splitBtn;

    private MultiplayerGameManager mgr;
    private int seatIndex = -1;
    private ulong steamId = 0;

    public void BindToSeat(MultiplayerGameManager manager, int index, ulong id)
    {
        mgr = manager; seatIndex = index; steamId = id;

        RemoveAll();

        if (seatIndex < 0) // no local human - keep disabled
        {
            SetBettingEnabled(false);
            SetTurnEnabled(false);
            return;
        }

        // Wire wagers
        if (bet1) bet1.onClick.AddListener(() => mgr.RequestWager(seatIndex, 1, steamId));
        if (bet5) bet5.onClick.AddListener(() => mgr.RequestWager(seatIndex, 5, steamId));
        if (bet10) bet10.onClick.AddListener(() => mgr.RequestWager(seatIndex, 10, steamId));
        if (betAllIn) betAllIn.onClick.AddListener(() => mgr.RequestWager(seatIndex, -1, steamId)); // -1 = all-in

        // Wire actions (Request* do the turn/ownership checks)
        if (hitBtn) hitBtn.onClick.AddListener(() => mgr.RequestHit(seatIndex, steamId));
        if (standBtn) standBtn.onClick.AddListener(() => mgr.RequestStand(seatIndex, steamId));
        if (doubleBtn) doubleBtn.onClick.AddListener(() => mgr.RequestDouble(seatIndex, steamId));
        if (splitBtn) splitBtn.onClick.AddListener(() => mgr.RequestSplit(seatIndex, steamId));
    }

    public void SetBettingEnabled(bool on)
    {
        if (bet1) bet1.interactable = on;
        if (bet5) bet5.interactable = on;
        if (bet10) bet10.interactable = on;
        if (betAllIn) betAllIn.interactable = on;
    }

    public void SetTurnEnabled(bool on)
    {
        if (hitBtn) hitBtn.interactable = on;
        if (standBtn) standBtn.interactable = on;
        if (doubleBtn) doubleBtn.interactable = on;
        if (splitBtn) splitBtn.interactable = on;
    }

    public void RemoveAll()
    {
        if (bet1) bet1.onClick.RemoveAllListeners();
        if (bet5) bet5.onClick.RemoveAllListeners();
        if (bet10) bet10.onClick.RemoveAllListeners();
        if (betAllIn) betAllIn.onClick.RemoveAllListeners();
        if (hitBtn) hitBtn.onClick.RemoveAllListeners();
        if (standBtn) standBtn.onClick.RemoveAllListeners();
        if (doubleBtn) doubleBtn.onClick.RemoveAllListeners();
        if (splitBtn) splitBtn.onClick.RemoveAllListeners();
    }
}
