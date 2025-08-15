using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Steamworks;


public class SeatRuntime
{
    public PlayerData player;
    public PlayerSeatUI ui;
    public ulong ownerSteamId; // 0 for bots/offline
}

public class MultiplayerGameManager : MonoBehaviour
{
    public CardManager cardManager;
    public TMPro.TextMeshProUGUI statusText;

    public List<PlayerSeatUI> seatUIs = new();

    // runtime state
    public List<PlayerData> players = new();
    public List<SeatRuntime> seats = new();   // <— single definition

    private int currentPlayerIndex = 0;
    private List<Card> dealerHand = new();
    private int dealerScore = 0;
    public List<TMPro.TextMeshProUGUI> playerCashTexts = new();
    private bool roundInProgress = false;

    private const int MIN_BET = 25;
    private bool HasMinBet(PlayerData p) => p.wager >= MIN_BET;
    private int GetIndex(PlayerData p) => players.IndexOf(p);

    public SharedSeatControls sharedControls;

    public TMPro.TextMeshProUGUI localCashBig;


    void Start()
    {
        //Build an empty table (SeatAssigner will populate ownership/humans vs. bots)
        BuildEmptyTable(seatUIs.Count);
         statusText.text = "Waiting for wagers..";
         UpdateCashUI();

    }

    public void BuildEmptyTable(int seatCount)
    {
        players.Clear();
        seats.Clear();

        for (int i = 0; i < seatCount; i++)
        {
            var p = new PlayerData($"Seat{i + 1}", true, 1_000_000); // default bot until SetSeatOwner
            players.Add(p);

            var s = new SeatRuntime
            {
                player = p,
                ui = seatUIs[i],
                ownerSteamId = 0UL
            };
            seats.Add(s);

            s.ui.Init(this, i, s.ownerSteamId, true);
            s.ui.UpdateMoneyUI(p.cash, p.wager);
        }
    }


    // Called by SeatAssigner when ownership is known
    public void SetSeatOwner(int index, ulong ownerId, string displayName, bool isBot)
    {
        var s = seats[index];
        s.ownerSteamId = ownerId;
        s.player.isBot = isBot;
        if (!string.IsNullOrEmpty(displayName))
            s.player.playerName = displayName;

        s.ui.Init(this, index, s.ownerSteamId, s.player.isBot);
        s.ui.UpdateMoneyUI(s.player.cash, s.player.wager);
    }

    public void BeginBettingPhase()
    {
        statusText.text = "Waiting for wagers...";
        //enable wagers for local seat only
        if (sharedControls)
            sharedControls.SetBettingEnabled(LocalSeatIndex >= 0);
    }

    private int LocalSeatIndex
    => seats.FindIndex(s => s.ownerSteamId == (SteamAPI.IsSteamRunning() ? SteamUser.GetSteamID().m_SteamID : 0)
                         && !s.player.isBot);
    
    private void SetTurnButtons(int activeSeat)
    {
        if (!sharedControls) return;
        sharedControls.SetTurnEnabled(activeSeat == LocalSeatIndex);
    }


    public void OnWager(PlayerData player, int amount)
    {
        if (amount <= 0) return;

        if (player.cash >= amount)
        {
            player.wager += amount;
            player.cash -= amount;

            statusText.text = (player.wager < MIN_BET)
                ? $"{player.playerName} wagered ${amount:N0} (min ${MIN_BET:N0} to play)"
                : $"{player.playerName} wagered ${amount:N0}";

            UpdateCashUI();
        }
        else
        {
            statusText.text = $"{player.playerName} doesn't have enough cash!";
        }
    }

    public void StartRound()
    {
        if (roundInProgress) return;

        bool anyEligible = false;
        foreach (var p in players) if (HasMinBet(p)) { anyEligible = true; break; }
        if (!anyEligible) { statusText.text = $"Need at least one player to wager min: ${MIN_BET:N0}."; return; }

        cardManager.ClearPlayerAreas();
        cardManager.ClearDealerArea();

        foreach (var p in players) { p.hand.Clear(); p.isDone = false; }
        dealerHand.Clear(); dealerScore = 0;

        for (int i = 0; i < players.Count; i++)
        {
            if (HasMinBet(players[i]))
            {
                players[i].hand.Add(cardManager.DealCardToPlayer(i, true, false));
                players[i].hand.Add(cardManager.DealCardToPlayer(i, true, false));
            }
            else players[i].isDone = true;
        }

        dealerHand.Add(cardManager.DealCardToDealer(false));
        dealerHand.Add(cardManager.DealCardToDealer(true));

        roundInProgress = true;

        currentPlayerIndex = 0;
        while (currentPlayerIndex < players.Count &&
               (players[currentPlayerIndex].isDone || players[currentPlayerIndex].hand.Count == 0))
            currentPlayerIndex++;

        if (currentPlayerIndex >= players.Count)
        {
            statusText.text = $"No eligible players this round (min ${MIN_BET}).";
            roundInProgress = false; return;
        }

        statusText.text = $"{players[currentPlayerIndex].playerName}'s turn!";
        StartCoroutine(HandleTurn(players[currentPlayerIndex]));
        UpdateCashUI();
    }

    private IEnumerator HandleTurn(PlayerData player)
    {
        yield return new WaitForSeconds(1f);

        if (player.hand.Count == 0) { NextPlayer(); yield break; }

        if (player.isBot)
        {
            int seatIndex = GetIndex(player);
            yield return StartCoroutine(BotTurn(player, seatIndex));
            NextPlayer();
        }
        else
        {
            statusText.text = $"{player.playerName}, choose an action!";
        }
    }

    public void OnHit()
    {
        if (!roundInProgress) { StartRound(); return; }
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count) return;

        var p = players[currentPlayerIndex];
        var c = cardManager.DealCardToPlayer(currentPlayerIndex, true, false);
        p.hand.Add(c);

        if (CalculateHandScore(p.hand) > 21)
        {
            p.isDone = true;
            statusText.text = $"{p.playerName} busted!";
            NextPlayer();
        }
    }

    public void OnStand()
    {
        if (!roundInProgress) return;
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count) return;

        var player = players[currentPlayerIndex];
        player.isDone = true;
        statusText.text = $"{player.playerName} stands.";
        NextPlayer();
    }

    private void NextPlayer()
    {
        currentPlayerIndex++;

        while (currentPlayerIndex < players.Count &&
               (players[currentPlayerIndex].isDone || players[currentPlayerIndex].hand.Count == 0))
            currentPlayerIndex++;

        if (currentPlayerIndex >= players.Count)
        {
            if (roundInProgress && dealerHand.Count >= 1)
                StartCoroutine(DealerTurn());
            else
                Debug.LogWarning("Tried to start DealerTurn without a valid dealer hand.");
        }
        else
        {
            StartCoroutine(HandleTurn(players[currentPlayerIndex]));
        }
    }

    private IEnumerator DealerTurn()
    {
        if (dealerHand.Count == 0) { Debug.LogError("DealerTurn called with empty dealerHand."); yield break; }

        dealerHand[0].ShowBack(false);
        dealerScore = CalculateHandScore(dealerHand);

        while (dealerScore < 17)
        {
            dealerHand.Add(cardManager.DealCardToDealer(true));
            dealerScore = CalculateHandScore(dealerHand);
            yield return new WaitForSeconds(1f);
        }

        ResolveBets();
        UpdateCashUI();

        roundInProgress = false;
        statusText.text = "Round over. Place wagers!";
    }

    private void ResolveBets()
    {
        int dealerFinal = CalculateHandScore(dealerHand);

        foreach (var player in players)
        {
            if (player.hand.Count == 0) { player.wager = 0; continue; }
            int score = CalculateHandScore(player.hand);
            if (score > 21) { player.wager = 0; continue; }

            if (dealerFinal > 21 || score > dealerFinal) player.cash += player.wager * 2;
            else if (score == dealerFinal) player.cash += player.wager;

            player.wager = 0;
        }
    }

    private int CalculateHandScore(List<Card> hand)
    {
        int total = 0, ace = 0;
        foreach (var c in hand) { total += c.realValue; if (c.value == 1) ace++; }
        while (total > 21 && ace > 0) { total -= 10; ace--; }
        return total;
    }

    private void UpdateLocalCashBig()
    {
        if (!localCashBig) return;
        int idx = LocalSeatIndex;
        localCashBig.text = (idx >= 0) ? $"{players[idx].cash:N0}" : "-";
    }


    private void UpdateCashUI()
    {
        for (int i = 0; i < players.Count && i < playerCashTexts.Count; i++)
            playerCashTexts[i].text = $"{players[i].playerName}: ${players[i].cash:N0}";

        UpdateLocalCashBig();
    }

    private IEnumerator BotTurn(PlayerData bot, int seatIndex)
    {
        statusText.text = $"{bot.playerName} thinking...";
        yield return new WaitForSeconds(1f);

        int dealerUp = (dealerHand.Count >= 2) ? dealerHand[1].realValue : 10;
        bool done = false;

        while (!done)
        {
            switch (GetBotDecision(bot.hand, dealerUp))
            {
                case BotAction.Hit:
                    bot.hand.Add(cardManager.DealCardToPlayer(seatIndex, true, false));
                    if (CalculateHandScore(bot.hand) > 21) { statusText.text = $"{bot.playerName} busted!"; done = true; }
                    break;

                case BotAction.Stand:
                    statusText.text = $"{bot.playerName} stands."; done = true;
                    break;

                case BotAction.Double:
                    if (bot.cash >= bot.wager)
                    {
                        bot.cash -= bot.wager; bot.wager *= 2;
                        bot.hand.Add(cardManager.DealCardToPlayer(seatIndex, true, false));
                        statusText.text = $"{bot.playerName} doubles.";
                    }
                    done = true;
                    break;

                case BotAction.Split:
                    done = true; // TODO later
                    break;
            }
            yield return new WaitForSeconds(1f);
        }
        bot.isDone = true;
    }

    private enum BotAction { Hit, Stand, Double, Split }
    private BotAction GetBotDecision(List<Card> hand, int dealerUpCardValue)
    {
        int score = CalculateHandScore(hand);
        bool isSoft = hand.Exists(c => c.realValue == 11);

        if (isSoft)
        {
            if (score >= 19) return BotAction.Stand;
            if (score == 18 && dealerUpCardValue >= 9) return BotAction.Hit;
            return BotAction.Stand;
        }
        if (score >= 17) return BotAction.Stand;
        if (score >= 13 && dealerUpCardValue <= 6) return BotAction.Stand;
        if (score == 12 && dealerUpCardValue is >= 4 and <= 6) return BotAction.Stand;
        if (score >= 10) return BotAction.Double;
        return BotAction.Hit;
    }

    // ---- Requests from UI (ownership/turn checks) ----

    private bool IsMySeat(int seatIndex, ulong caller)
        => seatIndex >= 0 && seatIndex < seats.Count && seats[seatIndex].ownerSteamId == caller;

    private bool IsCurrentTurn(int seatIndex) => seatIndex == currentPlayerIndex;

    public void RequestWager(int seatIndex, int amount, ulong callerSteamId)
    {
        if (roundInProgress) return;
        if (!IsMySeat(seatIndex, callerSteamId)) return;

        var p = seats[seatIndex].player;
        if (amount == -1) amount = p.cash; // all-in
        OnWager(p, amount);
        seats[seatIndex].ui.UpdateMoneyUI(p.cash, p.wager);
    }

    public void RequestHit(int seatIndex, ulong callerSteamId)
    {
        if (!roundInProgress) { StartRound(); return; }
        if (!IsCurrentTurn(seatIndex)) return;
        if (!IsMySeat(seatIndex, callerSteamId)) return;

        var p = seats[seatIndex].player;
        var c = cardManager.DealCardToPlayer(seatIndex, true, false);
        p.hand.Add(c);

        if (CalculateHandScore(p.hand) > 21)
        {
            p.isDone = true;
            statusText.text = $"{p.playerName} busted!";
            NextPlayer();
            SetTurnButtons(currentPlayerIndex);
        }
    }

    public void RequestStand(int seatIndex, ulong callerSteamId)
    {
        if (!roundInProgress) return;
        if (!IsCurrentTurn(seatIndex)) return;
        if (!IsMySeat(seatIndex, callerSteamId)) return;

        var p = seats[seatIndex].player;
        p.isDone = true;
        statusText.text = $"{p.playerName} stands.";
        NextPlayer();
        SetTurnButtons(currentPlayerIndex);
    }

    public void RequestDouble(int seatIndex, ulong callerSteamId)
    {
        if (!roundInProgress) return;
        if (!IsCurrentTurn(seatIndex)) return;
        if (!IsMySeat(seatIndex, callerSteamId)) return;

        var p = seats[seatIndex].player;
        if (p.cash >= p.wager && p.wager > 0)
        {
            p.cash -= p.wager; p.wager *= 2;
            p.hand.Add(cardManager.DealCardToPlayer(seatIndex, true, false));
            seats[seatIndex].ui.UpdateMoneyUI(p.cash, p.wager);
            NextPlayer();
            SetTurnButtons(currentPlayerIndex);
        }
    }

    public void RequestSplit(int seatIndex, ulong callerSteamId)
    {
        if (!roundInProgress || !IsCurrentTurn(seatIndex) || !IsMySeat(seatIndex, callerSteamId)) return;
        // TODO: split flow
    }
}