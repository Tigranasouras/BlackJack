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

    public List<PlayerData> players = new List<PlayerData>();
    private int currentPlayerIndex = 0;

    private List<Card> dealerHand = new List<Card>();
    private int dealerScore = 0;

    public List<TMPro.TextMeshProUGUI> playerCashTexts = new List<TMPro.TextMeshProUGUI>();

    private bool roundInProgress = false;

    private const int MIN_BET = 25;
    private bool HasMinBet(PlayerData p) => p.wager >= MIN_BET;

    private int GetIndex(PlayerData p) => players.IndexOf(p);

    public List<PlayerSeatUI> seatUIs = new List<PlayerSeatUI>();


    void Start()
    {
        players.Add(new PlayerData("Player1", false, 1000000));
        players.Add(new PlayerData("Bot1", true, 1000000));
        players.Add(new PlayerData("Bot2", true, 1000000));
        players.Add(new PlayerData("Bot3", true, 1000000));


        if (cardManager.playerAreas.Count < players.Count)
        {
            Debug.LogError("Not enough playerAreas set on CardManager for number of players.");
            return;
        }

        seats.Clear();
        for(int i = 0; i < players.Count; i++)
        {
            var s = new SeatRuntime
            {
                player = players[i],
                ui = seatUIs[i],
                ownerSteamId = players[i].isBot? 0UL : SteamUser.GetSteamID().m_SteamID //local test: seat 0 is you
            };
            seats.Add(s);

            s.ui.Init(this, i, s.ownerSteamId, players[i].isBot);
            s.ui.UpdateMoneyUI(players[i].cash, players[i].wager);
        }


        UpdateCashUI();

        statusText.text = "Waiting for wagers..";
        SetAllBetting(true);
    }

    public void OnWager(PlayerData player, int amount)
    {
        if (amount <= 0) return;

        if (player.cash >= amount)
        {
            player.wager += amount;
            player.cash -= amount;

            //Feedback if they're still short of the table minimum
            if (player.wager < MIN_BET)
            {
                statusText.text = $"{player.playerName} wagered ${amount:N0} (min ${MIN_BET:N0} to play)";
                //Handle Player leaving table.
            }
            else {
                statusText.text = $"{player.playerName} wagered ${amount:N0}";
            }

            UpdateCashUI();

        }
        else
        {
            statusText.text = $"{player.playerName} doesn't have enough cash!";
        }
    }

    public void StartRound()
    {
        if (roundInProgress) return; //avoid double starts

        //Require at least one eligible player
        bool anyEligible = false;
        foreach (var p in players) if (HasMinBet(p)) {  anyEligible  |= true; break; }
        if (!anyEligible)
        {
            statusText.text = $"Need at least one player to wager min:  ${MIN_BET:N0}.";
            return;
        }


        //clear visuals
        cardManager.ClearPlayerAreas();
        cardManager.ClearDealerArea();


        // reset data
        foreach (var p in players) { p.hand.Clear(); p.isDone = false; }
        dealerHand.Clear(); dealerScore = 0;

        // Deal only to players who met the minimum; others sit out this round
        for (int i = 0; i < players.Count; i++)
        {
            if (HasMinBet(players[i]))
            {
                players[i].hand.Add(cardManager.DealCardToPlayer(i, true, false)); // main
                players[i].hand.Add(cardManager.DealCardToPlayer(i, true, false)); // main

            }
            else
            {
                players[i].isDone = true; //They won't get a turn
            }
            
        }
        //Dealer Cards
        dealerHand.Add(cardManager.DealCardToDealer(false)); // hole
        dealerHand.Add(cardManager.DealCardToDealer(true));  // upcard

        roundInProgress = true;

        //Start on first eligible player with cards
        currentPlayerIndex = 0;
        while (currentPlayerIndex < players.Count && (players[currentPlayerIndex].isDone || players[currentPlayerIndex].hand.Count == 0))
            currentPlayerIndex++;
        if(currentPlayerIndex >=  players.Count)
        {
            //Edge case: somehow no one ended up eligible
            statusText.text = $"No eligible players this round (min ${MIN_BET}).";
            roundInProgress = false;
            return;

        }

        statusText.text = $"{players[currentPlayerIndex].playerName}'s turn!";
        StartCoroutine(HandleTurn(players[currentPlayerIndex]));

        UpdateCashUI();
    }

    private IEnumerator HandleTurn(PlayerData player)
    {
        yield return new WaitForSeconds(1f);

        if (player.hand.Count == 0) { NextPlayer(); yield break; } //sat out

        if (player.isBot)
        {
            int seatIndex = GetIndex(player);
            yield return StartCoroutine(BotTurn(player, seatIndex));
            NextPlayer();
        }
        else
        {
            //Human Wait for input (Hit/Stand buttons call OnHit/OnStand)
            statusText.text = $"{player.playerName}, choose an action!";

        }
    }

    public void OnHit()
    {
        if (!roundInProgress) { StartRound(); return; }
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count) return;

        var p = players[currentPlayerIndex];
        int idx = currentPlayerIndex;

        Card c = cardManager.DealCardToPlayer(idx, true, false);
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
        if (!roundInProgress) return; //prevent early Stand causing DealerTurn()
        if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count) return;

        var player = players[currentPlayerIndex];
        player.isDone = true;
        statusText.text = $"{player.playerName} stands.";
        NextPlayer();
    }

    private void NextPlayer()
    {
        currentPlayerIndex++;


        //Skip Players that might be already done (bots can set isDone)
        while (currentPlayerIndex < players.Count &&
          (players[currentPlayerIndex].isDone || players[currentPlayerIndex].hand.Count == 0))
            currentPlayerIndex++;

        if (currentPlayerIndex >= players.Count)
        {
            if (roundInProgress && dealerHand.Count >= 1) // must have at least the hole card
            {
                StartCoroutine(DealerTurn());
            }
            else
            {
                Debug.LogWarning("Tried to start DealerTurn without a valid dealer hand.");
            }
        }
        else
        {
            StartCoroutine(HandleTurn(players[currentPlayerIndex]));
        }

    }

    private IEnumerator DealerTurn()
    {

        if(dealerHand.Count == 0)
        {
            Debug.LogError("DealerTurn called with empty dealerHand.");
            yield break;
        }

        //Reveal hole card safely
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

            if (player.hand.Count == 0) { player.wager = 0; continue; } // sat out
            int score = CalculateHandScore(player.hand);
            if (score > 21)
            {
                player.wager = 0;
                continue;
            }

            if (dealerFinal > 21 || score > dealerFinal)
            {
                player.cash += player.wager * 2;
            }
            else if (score == dealerFinal)
            {
                player.cash += player.wager; // push
            }

            player.wager = 0;
        }
    }


    private int CalculateHandScore(List<Card> hand)
    {
        int total = 0;
        int aceCount = 0;
        foreach(Card c in hand)
        {
            total += c.realValue;
            if (c.value == 1) aceCount++;
        }
        while (total > 21 && aceCount >0)
        {
            total -= 10;
            aceCount--;
        }
        return total;
    }

    private void UpdateCashUI()
    {
        for(int i = 0; i < players.Count && i < playerCashTexts.Count; i++)
        {
            playerCashTexts[i].text = $"{players[i].playerName}: ${players[i].cash:N0}";
        }
    }


    private IEnumerator BotTurn(PlayerData bot, int seatIndex)
    {
        statusText.text = $"{bot.playerName} thinking...";
        yield return new WaitForSeconds(1f);

        int dealerUpCardValue = (dealerHand.Count >= 2) ? dealerHand[1].realValue : 10;
        bool done = false;

        while (!done)
        {
            BotAction action = GetBotDecision(bot.hand, dealerUpCardValue);
            switch (action)
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
                    // later: use DealCardToPlayer(seatIndex, true, true) for split
                    done = true;
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
        if (score == 12 && dealerUpCardValue >= 4 && dealerUpCardValue <= 6) return BotAction.Stand;
        if (score >= 10) return BotAction.Double;

        return BotAction.Hit;
    }



    //Multiplayer stuff



    public List<SeatRuntime> seats = new(); // size = 4 in Inspector

    private ulong LocalSteamId => SteamAPI.IsSteamRunning() ? SteamUser.GetSteamID().m_SteamID : 0;

    private bool IsMySeat(int seatIndex, ulong caller)
    {
        if (seatIndex < 0 || seatIndex >= seats.Count) return false;
        return seats[seatIndex].ownerSteamId == caller;
    }

    private bool IsCurrentTurn(int seatIndex) => seatIndex == currentPlayerIndex;

    // ----- Requests from UI -----

    public void RequestWager(int seatIndex, int amount, ulong callerSteamId)
    {
        if (roundInProgress) return;
        if (!IsMySeat(seatIndex, callerSteamId)) return;

        var p = seats[seatIndex].player;
        if (amount == -1) amount = p.cash; // all-in

        OnWager(p, amount);                          // your existing logic
        seats[seatIndex].ui.UpdateMoneyUI(p.cash, p.wager);
    }

    public void RequestHit(int seatIndex, ulong callerSteamId)
    {
        if (!roundInProgress) { StartRound(); return; }
        if (!IsCurrentTurn(seatIndex)) return;
        if (!IsMySeat(seatIndex, callerSteamId)) return;

        // reuse your OnHit logic but target by index
        var p = seats[seatIndex].player;
        var c = cardManager.DealCardToPlayer(seatIndex, true, false);
        p.hand.Add(c);

        if (CalculateHandScore(p.hand) > 21) { 
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
            NextPlayer(); // BJ rule: double = one card then stand
            SetTurnButtons(currentPlayerIndex);
        }
    }

    public void RequestSplit(int seatIndex, ulong callerSteamId)
    {
        // later: enforce identical ranks, enough cash, etc.
        if (!roundInProgress || !IsCurrentTurn(seatIndex) || !IsMySeat(seatIndex, callerSteamId)) return;
        // call your split flow using DealCardToPlayer(seatIndex, true, true)
    }

    private void SetAllBetting(bool enabled)
    {
        foreach (var s in seats)
            s.ui.SetBettingEnabled(enabled && s.ownerSteamId != 0 && !s.player.isBot);
    }

    private void SetTurnButtons(int activeSeat)
    {
        for (int i = 0; i < seats.Count; i++)
        {
            bool myTurn = (i == activeSeat) && seats[i].ownerSteamId != 0 && !seats[i].player.isBot;
            seats[i].ui.SetInteractable(myTurn);
        }
    }

}
