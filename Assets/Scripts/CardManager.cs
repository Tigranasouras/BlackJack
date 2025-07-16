using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CardManager : MonoBehaviour
{
    public GameObject cardPrefab; //Card visuals
    public Transform dealerArea, deckArea;

    public Transform firstHandContainer;
    public Transform splitHandContainer;
    public enum HandType { PlayerMain, PlayerSplit, Dealer }


    public TMPro.TextMeshProUGUI cardCountText;
    

    private List<CardData> deck = new List<CardData>();
    private int runningCount = 0;

    //[Range(1,8]
    public int numerOfDecks = 6;
    

    void Start()
    {
        GenerateDeck();
        ShuffleDeck();
    }

 

    public Card DealCard(bool faceUp, HandType handType)
    {
        CheckShuffleNeeded();

        if (deck.Count == 0)
        {
            return null;
        }

        CardData drawnCard = deck[0]; //first card in deck
        deck.RemoveAt(0); // remove top
        runningCount += drawnCard.countValue; // add to running total
        UpdateCardCountUI();

        Transform parentArea = handType switch
        {
            HandType.PlayerMain => firstHandContainer,
            HandType.PlayerSplit => splitHandContainer,
            HandType.Dealer => dealerArea,
            _ => deckArea
        };

        GameObject cardGO = Instantiate(cardPrefab, parentArea); // set parent
        Card cardComponent = cardGO.GetComponent<Card>();
        cardComponent.SetCard(drawnCard.suit, drawnCard.value);
        cardComponent.ShowBack(!faceUp);
        return cardComponent; //Return the drawn card so GameManager can access its value
    }



    void UpdateCardCountUI()
    {
        cardCountText.text = "Running: " + runningCount + "\nTrue: " + GetTrueCount().ToString("0.0");
    }

    void GenerateDeck()
    {
        deck.Clear();

        for (int d = 0; d < numerOfDecks; d++)
        {
            string[] suits = { "Spades", "Hearts", "Clubs", "Diamonds" };
            foreach(string suit in suits)
            {
                for (int i = 1; i <= 13; i++)
                {


                    int cardValue = Mathf.Min(i, 10); // Face cards worth 10
                    int actualValue = (i == 1) ? 11 : (i >= 11 ? 10 : i); // Face cards = 10, Ace = 11, others = number
                    var cardData = new CardData(suit, actualValue);

                    deck.Add(cardData);
                }
            }
            
        }
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++) // thank god for algorithms
        {
            CardData temp = deck[i];
            int rand = Random.Range(i, deck.Count);
            deck[i] = deck[rand];
            deck[rand] = temp;
        }
    }

    public int GetRunningCount()
    {
        return runningCount;
    }

    public float GetTrueCount()
    {
        float decksRemaining = Mathf.Max(deck.Count / 52f, 1f);
        return runningCount / decksRemaining;
    }


    void CheckShuffleNeeded()
    {
        float penetration = 1f - (deck.Count / (float)(numerOfDecks * 52));
        if (penetration >= 0.75f)
        {
            GenerateDeck();
            ShuffleDeck();
            runningCount = 0;
            Debug.Log("Deck reshuffled due to penetration.");
        }
    }

}
