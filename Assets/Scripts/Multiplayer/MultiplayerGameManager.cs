using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MultiplayerGameManager : MonoBehaviour
{
    public CardManager cardManager;
    public TMPro.TextMeshProUGUI statusText;

    public List<PlayerData> players = new List<PlayerData>();
    private int currentPlayerIndex = 0;

    private List<Card> dealerHand = new List<Card>();
    private int dealerScore = 0;

    private bool roundInProgress = false;

    void Start()
    {
        players.Add(new PlayerData("Player1", false, 1000000));
        players.Add(new PlayerData("Bot1", true, 1000000));
        players.Add(new PlayerData("Bot2", true, 1000000));


        statusText.text = "Waiting for wagers..";
    }

    public void OnWager(PlayerData player, int amount)
    {
        if (player.cash >= amount)
        {
            player.wager += amount;
            player.cash -= amount;
            statusText.text = $"{player.playerName} wagered ${amount:N0}";
        }
        else
        {
            statusText.text = $"{player.playerName} doesn't gave enough cash!";
        }
    }

    public void StartRound()
    {
        //Reset hands and dealer
        foreach(var player in players)
        {
            player.hand.Clear();
            player.isDone = false;
        }
        dealerHand.Clear();
        dealerScore = 0;

        //Deal Initial cards
        foreach(var player in players)
        {
            player.hand.Add(cardManager.DealCard(true, CardManager.HandType.PlayerMain)); //face down
            player.hand.Add(cardManager.DealCard(true, CardManager.HandType.PlayerMain)); //upcard
        }
        dealerHand.Add(cardManager.DealCard(false, CardManager.HandType.Dealer)); // face down
        dealerHand.Add(cardManager.DealCard(true, CardManager.HandType.Dealer)); // upcard

        roundInProgress = true;
        currentPlayerIndex = 0;
        statusText.text = $"{players[currentPlayerIndex].playerName}'s turn!";
        StartCoroutine(HandleTurn(players[currentPlayerIndex]));
    }

    private IEnumerator HandleTurn(PlayerData player)
    {
        yield return new WaitForSeconds(1f);

        if (player.isBot)
        {
            yield return StartCoroutine(BotTurn(player));
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
        var player = players[currentPlayerIndex];
        Card card = cardManager.DealCard(true, CardManager.HandType.PlayerMain);
        player.hand.Add(card);

        if(CalculateHandScore(player.hand) > 21)
        {
            player.isDone = true;
            statusText.text = $"{player.playerName} busted!";
            NextPlayer();
        }
    }

    public void OnStand()
    {
        var player = players[currentPlayerIndex];
        player.isDone = true;
        statusText.text = $"{player.playerName} stands.";
        NextPlayer();
    }

    private void NextPlayer()
    {
        currentPlayerIndex++;
        if(currentPlayerIndex >= players.Count)
        {
            StartCoroutine(DealerTurn());
        }
        else
        {
            StartCoroutine(HandleTurn(players[currentPlayerIndex]));
        }

    }

    private IEnumerator DealerTurn()
    {
        //Reveal hole card
        dealerHand[0].ShowBack(false);
        dealerScore = CalculateHandScore(dealerHand);

        while (dealerScore < 17)
        {
            dealerHand.Add(cardManager.DealCard(true, CardManager.HandType.Dealer));
            dealerScore = CalculateHandScore(dealerHand);
            yield return new WaitForSeconds(1f);
        }

        ResolveBets();
        roundInProgress = false;
        statusText.text = "Round over. Place wagers!";
    }

    private void ResolveBets()
    {
        int dealerFinal = CalculateHandScore(dealerHand);

        foreach(var player in players)
        {
            int score = CalculateHandScore(player.hand);
            if(score > 21) 
                continue;
                if (dealerFinal > 21 || score > dealerFinal)
                    player.cash += player.wager * 2;
                else if (score == dealerFinal)
                    player.cash += player.wager; // push
                // else: dealer wins, nothing returned
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


    private IEnumerator BotTurn(PlayerData bot)
    {
        statusText.text = $"{bot.playerName} thinking...";
        yield return new WaitForSeconds(1f);

        int dealerUpCardValue = dealerHand[1].realValue;
        bool done = false;

        while(!done)
        {
            BotAction action = GetBotDecision(bot.hand, dealerUpCardValue);

            switch(action)
            {
                case BotAction.Hit:
                    bot.hand.Add(cardManager.DealCard(true, CardManager.HandType.PlayerMain));
                    if(CalculateHandScore(bot.hand) >21)
                    {
                        statusText.text = $"{bot.playerName} busted!";
                        done = true;
                    }
                    break;
                case BotAction.Stand:
                    statusText.text = $"{bot.playerName} stands.";
                    done = true;
                    break;
                case BotAction.Double:
                    if (bot.cash >= bot.wager)
                    {
                        bot.cash -= bot.wager;
                        bot.wager *= 2;
                        bot.hand.Add(cardManager.DealCard(true, CardManager.HandType.PlayerMain));
                        statusText.text = $"{bot.playerName} doubles.";
                    }
                    done = true;
                    break;
                case BotAction.Split:
                    //simplified split handling
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


    
}
