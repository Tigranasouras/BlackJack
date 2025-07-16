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

    public TMPro.TextMeshProUGUI cashCountText;
    public TMPro.TextMeshProUGUI wagerText;

    bool handEnd = false;
    bool handStart = false;
    bool wagered = false;
    bool wagerClose = false;

    private List<Card> playerHand = new List<Card>();
    private List<Card> dealerHand = new List<Card>();

    private List<Card> splitHand = new List<Card>();
    public UnityEngine.UI.Button splitButton;
    private bool isSplitActive = false;
    private bool playingSecondHand = false;
    private int splitScore = 0;
    private int splitWager = 0;



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

    public void DealInitialCards()
    {
        if (dealerHand.Count == 0 && playerHand.Count == 0)
        {
            // 1st card to player
            Card playerCard1 = cardManager.DealCard(true);
            playerHand.Add(playerCard1);

            // 1st card to dealer - face-down
            Card dealerCard1 = cardManager.DealCard(false);
            dealerCard1.ShowBack(true);
            dealerHand.Add(dealerCard1);

            // 2nd card to player
            Card playerCard2 = cardManager.DealCard(true);
            playerHand.Add(playerCard2);

            // 2nd card to dealer - face-up
            Card dealerCard2 = cardManager.DealCard(false);
            dealerCard2.ShowBack(false);
            dealerHand.Add(dealerCard2);

            playerScore = CalculateHandScore(playerHand);
            dealerScore = CalculateHandScore(dealerHand);

            //Check for player blackjack
            if (playerScore == 21)
            {
                statusText.text = "Blackjack! You win!";
                cash += Mathf.RoundToInt(playerWager * 2.5f);
                UpdateCashText();
                handEnd = true;
                handStart = false;
                wagerClose = true;
                playerWager = 0;
                UpdateWagerText();
            }
        }
        UpdateSplitButtonState();

    }

    public void onHit()
    {
        if (!handEnd && wagered)
        {
            wagerClose = true;
            if (!handStart)
            {
                DealInitialCards();
                handStart = true;
                UpdateSplitButtonState();
                return;
            }

            if (isSplitActive && playingSecondHand)
            {
                Card newCard = cardManager.DealCard(true);
                splitHand.Add(newCard);
                splitScore = CalculateHandScore(splitHand);
                if (splitScore > 21)
                {
                    statusText.text = "Split hand busted!";
                    onStand(); // Automatically end if busted
                }
            }
            else
            {
                Card newCard = cardManager.DealCard(true);
                playerHand.Add(newCard);
                playerScore = CalculateHandScore(playerHand);
                if (playerScore > 21)
                {
                    if (isSplitActive)
                    {
                        statusText.text = "First hand busted. Moving to split hand.";
                        playingSecondHand = true;
                    }
                    else
                    {
                        statusText.text = "You busted.";
                        handEnd = true;
                        handStart = false;
                        playerWager = 0;
                        UpdateWagerText();
                        UpdateCashText();
                    }
                }
            }

        }
        else
        {
            statusText.text = "Wager or NextHand!";
        }
    }

    public void onStand()
    {
        if (handStart && wagered)
        {
            if (isSplitActive && !playingSecondHand)
            {
                playingSecondHand = true;
                statusText.text = "Playing Split Hand";
                return;
            }

            if (dealerHand.Count > 0)
                dealerHand[0].ShowBack(false);

            while (dealerScore < 17)
            {
                Card dealerCard = cardManager.DealCard(false);
                dealerHand.Add(dealerCard);
                dealerScore = CalculateHandScore(dealerHand);
            }

            int winnings = 0;
            if (playerScore <= 21)
                winnings += CompareHands(playerScore, playerWager);
            if (isSplitActive && splitScore <= 21)
                winnings += CompareHands(splitScore, splitWager);

            cash += winnings;
            UpdateCashText();
            playerWager = 0;
            UpdateWagerText();
            handEnd = true;
            handStart = false;
            isSplitActive = false;
            playingSecondHand = false;
        }
        else
        {
            statusText.text = "Wager or NextHand!";
        }


    }


    public void onDouble()
    {
        if (isSplitActive && playingSecondHand)
        {
            if (cash < splitWager)
            {
                statusText.text = "Not enough cash for split double!";
                return;
            }

            cash -= splitWager;
            splitWager *= 2;
            UpdateCashText();
            UpdateWagerText();

            Card newCard = cardManager.DealCard(true);
            splitHand.Add(newCard);
            splitScore = CalculateHandScore(splitHand);

            if (splitScore > 21)
            {
                statusText.text = "Split hand busted!";
            }

            onStand(); // Force stand after double
            return;
        }

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

    public void OnSplit()
    {
        if (!handStart || playerHand.Count != 2 || playerHand[0].value != playerHand[1].value || cash < playerWager)
        {
            statusText.text = "Can't split!";
            return;
        }

        // Deduct wager for 2nd hand
        cash -= playerWager;
        splitWager = playerWager;
        UpdateCashText();

        isSplitActive = true;
        playingSecondHand = false;

        // Move second card to split hand
        Card splitCard = playerHand[1];
        playerHand.RemoveAt(1);
        splitHand.Add(splitCard);

        // Deal one card to each hand
        playerHand.Add(cardManager.DealCard(true));
        splitHand.Add(cardManager.DealCard(true));

        playerScore = CalculateHandScore(playerHand);
        splitScore = CalculateHandScore(splitHand);

        statusText.text = "Playing First Hand";
        UpdateSplitButtonState();

    }

    public void resetGame()
    {
        handEnd = false;
        wagered = false;
        wagerClose = false;
        handStart = false;
        playerHand.Clear();
        dealerHand.Clear();
        splitHand.Clear();


        playerScore = 0;
        dealerScore = 0;
        playerWager = 0;
        splitScore = 0;
        splitWager = 0;

        ClearCards(cardManager.playerArea);
        ClearCards(cardManager.dealerArea);

        cashCountText.text = cash.ToString("N0");
        wagerText.text = "Wager: $" + playerWager.ToString("N0");

        statusText.text = "Waiting on player!";
        UpdateSplitButtonState();

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
            if (amount == -15)
            {
                amount = cash;
            }

            playerWager += amount;
            cash -= amount;
            UpdateCashText();
            UpdateWagerText();
            UpdateSplitButtonState();

            if (!wagered)
            {
                wagered = true;
                statusText.text = "Waiting on Player!";
            }


        }
        else if (cash < amount)
        {
            statusText.text = "Not enough Cash!";
        }


    }

    public void AddWagerButtonAllIn()
    {
        int money = cash;

        addWager(money);
    }

    void ClearCards(Transform area)
    {
        foreach (Transform child in area)
        {
            Destroy(child.gameObject);
        }
    }

    private int CompareHands(int score, int wagerAmount)
    {
        if (dealerScore > 21 || score > dealerScore)
        {
            statusText.text = "Hand wins!";
            return wagerAmount * 2;
        }
        else if (score == dealerScore)
        {
            statusText.text = "Push!";
            return wagerAmount;
        }
        else
        {
            statusText.text = "Dealer wins!";
            return 0;
        }

    }

    private void UpdateSplitButtonState()
    {
        bool canSplit =
            playerHand.Count == 2 &&
            playerHand[0].value == playerHand[1].value &&
            cash >= playerWager &&
            !isSplitActive;

        splitButton.gameObject.SetActive(canSplit); 
    }

}
