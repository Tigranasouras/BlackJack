using System.Collections;
using System.Collections.Generic;
using TMPro.Examples;
using Unity.VisualScripting;
using UnityEngine;

public class gameManager : MonoBehaviour
{
    public int playerScore = 0;
    public int dealerScore = 0;
    public int cash = 1000000;
    public int playerWager = 0;
    public TMPro.TextMeshProUGUI statusText;
    public CardManager cardManager;
    public TMPro.TextMeshProUGUI dealerCountText;
    public TMPro.TextMeshProUGUI playerCountText;

    public TMPro.TextMeshProUGUI cashCountText;
    public TMPro.TextMeshProUGUI wagerText;

    bool handEnd = false;
    bool handStart = false;
    bool wagered = false;
    bool wagerClose = false;

    bool card1 = false;

    private List<Card> playerHand = new List<Card>();
    private List<Card> dealerHand = new List<Card>();



    public int CalculateHandScore(List<Card> hand)
    {
        int total = 0;
        int aceCount = 0;

        foreach (Card card in hand) 
        {
            total += card.value;
            if (card.value == 11) aceCount++; //Count # of Aces (treated as 11 initially)

        }
        //Downgrade Ace from 11 to 1 if total is over 21
        while (total > 21 && aceCount > 0)
        {
            total -= 10; // Convert one Ace from 11 to 1
            aceCount--;

        }

        return total;
    }

    public void onHit()
    {
        if (!handEnd && wagered)
        {
            wagerClose = true;
            handStart = true;

            if(card1 == false) // to give 1 card to dealer
            { 
            Card dealerCard = cardManager.DealCard(false); //gets the card dealt
             dealerHand.Add(dealerCard); // adds card to DealerHand List
             dealerScore = CalculateHandScore(dealerHand);
            dealerCountText.text = "Dealer Count: " + dealerScore.ToString("00");
            card1 = true;
            }



            Card playerCard = cardManager.DealCard(true); //gets the card dealt
            playerHand.Add(playerCard);
            playerScore += CalculateHandScore(playerHand); // Score of dealt card
            playerCountText.text = "Player Count: " + playerScore.ToString("00");


            if (playerScore > 21)
            {
                statusText.text = "You busted.";
                // no need to update cash
                UpdateCashText();
                handEnd = true;
                handStart = false;
                playerWager = 0;
                UpdateWagerText();
                card1 = false;

            }
        }
        else {
            statusText.text = "Wager or NextHand!";
        }
    }

    public void onStand()
    {
        if (handStart && wagered)
        {


            while (dealerScore < 17)
        {
            Card dealerCard = cardManager.DealCard(false); //Get the dealer Card
                dealerHand.Add(dealerCard); // adds card to DealerHand List
                dealerScore = CalculateHandScore(dealerHand);
                dealerCountText.text = "Dealer Count: " + dealerScore.ToString("00");

        }
        if (dealerScore > 21 || playerScore > dealerScore)
        {
            normalWin();
            
        }
        else if (playerScore < dealerScore)
        {
            loose();
            

        }
        else
        {
            push();
            

        }
      }else
        {
            statusText.text = "Wager or NextHand!";
        }


    }


    public void onDouble()
    {
        if (wagered && handStart && cash >= playerWager)
        {
            cash -= playerWager;
            playerWager *= 2;
            UpdateCashText();
            UpdateWagerText();
            wagerClose = true;
            handStart = true;

            Card playerCard = cardManager.DealCard(true); // get the card Dealt
            playerHand.Add(playerCard); // adds card to DealerHand List
            playerScore = CalculateHandScore(playerHand);
            playerCountText.text = "Player Count: " + playerScore.ToString("00");

            if (playerScore > 21)
            {
                statusText.text = "You busted!";
                handEnd = true;
                handStart = false;
                playerWager = 0;
                UpdateWagerText();
                UpdateCashText();
            }
            else
            {
                // Automatically stand if not busted
                onStand();
            }
        }
        else if (cash < playerWager)
        {
            statusText.text = "Not enough cash!";
        }
        else if (!wagered || !handStart)
        {
            statusText.text = "Can't double now!";
        }
    }


    int DrawRandomCardScore() { return Random.Range(1, 11);}

    public void resetGame()
    {
        handEnd = false;
        wagered = false;
        wagerClose = false;
        handStart = false;
        card1 = false;
        playerHand.Clear();
        dealerHand.Clear();

        playerScore = 0;
        dealerScore = 0;
        playerWager = 0;
     


        playerCountText.text = "Player Count: 00";
        dealerCountText.text = "Dealer Count: 00";
        cashCountText.text = cash.ToString("N0");
        wagerText.text = "Wager: $" + playerWager.ToString("N0");

        statusText.text = "Waitng on player!";

    }

    private void push()
    {
        statusText.text = "Push!";
        cash += playerWager; // returns amount wagered
        UpdateCashText();
        handEnd = true;
        handStart = false;
        playerWager = 0;
        UpdateWagerText();
        card1 = false;
    }


    private void normalWin()
    {
        statusText.text = "You win!";
        cash += playerWager * 2;
        UpdateCashText();
        handEnd = true;
        handStart = false;
        playerWager = 0;
        UpdateWagerText();
        card1 = false;

    }

    private void blackJackwin()
    {
        statusText.text = "BlackJack! You win!";
        cash += Mathf.RoundToInt(playerWager * 2.5f);
        UpdateCashText();
        handEnd = true;
        handStart = false;
        playerWager = 0;
        UpdateWagerText();
        card1 = false;

    }

    private void loose()
    {
        statusText.text = "Dealer wins!";
        // no need to update cash
        UpdateCashText();
        handEnd = true;
        handStart = false;
        playerWager = 0;
        UpdateWagerText();
        card1 = false;
    }

    private void UpdateCashText()
    {
        cashCountText.text = cash.ToString("N0");
    }

    private void UpdateWagerText()
    {
        wagerText.text = "Wager: $" + playerWager.ToString("N0");
    }

    public void addWager(int amount)
    {
        if (!wagerClose && cash >= amount)
        {
            if(amount == -15){
                amount = cash;
            }

            playerWager += amount;
            cash -= amount;
            UpdateCashText();
            UpdateWagerText();
            if (!wagered) {
                wagered = true;
                statusText.text = "Waiting on Player!";
            }


        }
        else if(cash < amount)
        {
            statusText.text = "Not enough Cash!";
        }

        
    }

    public void AddWagerButtonAllIn()
    {
        int money = cash;

        addWager(money);
    }


}
