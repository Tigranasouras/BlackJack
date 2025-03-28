using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public GameObject cardPrefab; //Card visuals
    public Transform dealerArea, playerArea;
    public TMPro.TextMeshProUGUI cardCountText;
    

    private List<Card> deck = new List<Card>();
    private int runningCount = 0;
    

    void Start()
    {
        GenerateDeck();
        ShuffleDeck();
    }

    public void DealCard(bool toPlayer)
    {
        if (deck.Count == 0)
        {
            return;
        }

        Card drawnCard = deck[0]; //first card in deck
        deck.RemoveAt(0); // remove top

        runningCount += drawnCard.countValue; // add to running total
        UpdateCardCountUI();

        GameObject cardGO = Instantiate(cardPrefab, toPlayer ? playerArea : dealerArea); // pick between player or dealer
        cardGO.GetComponent<Card>().SetCard(drawnCard.suit, drawnCard.value); // assigns value
    }

    void UpdateCardCountUI()
    {
        cardCountText.text = "Card Count: " + runningCount;
    }

    void GenerateDeck()
    {
        string[] suits = { "Spades", "Hearts", "Clubs", "Diamonds" };

        foreach (string suit in suits)
        {
            for (int i = 1; i <= 13; i++)
            {
                GameObject cardObj = Instantiate(cardPrefab);
                Card card = cardObj.GetComponent<Card>();
                int cardValue = Mathf.Min(i, 10); // Face cards worth 10
                card.SetCard(suit, i == 1 ? 11 : cardValue); // Ace = 11 by default
                deck.Add(card);
                cardObj.SetActive(true);
            }
        }
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++) // thank god for algorithms
        {
            Card temp = deck[i];
            int rand = Random.Range(i, deck.Count);
            deck[i] = deck[rand];
            deck[rand] = temp;
        }
    }

    public int GetRunningCount()
    {
        return runningCount;
    }
}
