using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("Prefabs & Common Areas")]
    public GameObject cardPrefab;
    public Transform dealerArea;
    public Transform deckArea;

    [Header("Per-Player Areas (size = max players, e.g., 4)")]
    public List<PlayerHandContainers> playerAreas = new List<PlayerHandContainers>();

    public enum HandType { Dealer, PlayerMain, PlayerSplit }

    public TMPro.TextMeshProUGUI cardCountText;

    private List<CardData> deck = new List<CardData>();
    private int runningCount = 0;
    public int numerOfDecks = 6;

    void Start()
    {
        GenerateDeck();
        ShuffleDeck();
    }

    // Multiplayer-aware deal
    public Card DealCardToPlayer(int playerIndex, bool faceUp, bool toSplit)
    {
        CheckShuffleNeeded();
        if (deck.Count == 0) return null;

        if (playerIndex < 0 || playerIndex >= playerAreas.Count)
        {
            Debug.LogError($"DealCardToPlayer: playerIndex {playerIndex} out of range.");
            return null;
        }

        var drawn = DrawTop();
        Transform parent = toSplit ? playerAreas[playerIndex].split : playerAreas[playerIndex].main;

        return SpawnCard(drawn, parent, faceUp);
    }

    public Card DealCardToDealer(bool faceUp)
    {
        CheckShuffleNeeded();
        if (deck.Count == 0) return null;

        var drawn = DrawTop();
        return SpawnCard(drawn, dealerArea, faceUp);
    }

    // Back-compat (single player)
    public Card DealCard(bool faceUp, HandType handType, int playerIndex = 0)
    {
        switch (handType)
        {
            case HandType.Dealer:
                return DealCardToDealer(faceUp);
            case HandType.PlayerMain:
                return DealCardToPlayer(playerIndex, faceUp, false);
            case HandType.PlayerSplit:
                return DealCardToPlayer(playerIndex, faceUp, true);
            default:
                return null;
        }
    }

    // Helpers
    private CardData DrawTop()
    {
        CardData c = deck[0];
        deck.RemoveAt(0);
        runningCount += c.countValue;        // assumes CardData has countValue
        UpdateCardCountUI();
        return c;
    }

    public void MoveCardBetweenHands(int playerIndex, Card card, bool toSplit)
    {
        if (playerIndex < 0 || playerIndex >= playerAreas.Count || card == null) return;
        Transform dst = toSplit ? playerAreas[playerIndex].split : playerAreas[playerIndex].main;

        //reparent the existing card so it visually moves to the other lane
        card.transform.SetParent(dst, false);
        card.transform.SetAsLastSibling(); //keeps draw order nice

    }

    private Card SpawnCard(CardData data, Transform parent, bool faceUp)
    {
        if (parent == null) parent = deckArea;

        var go = Instantiate(cardPrefab, parent);
        var card = go.GetComponent<Card>();
        card.SetCard(data.suit, data.value);
        card.ShowBack(!faceUp);
        return card;
    }

    private void UpdateCardCountUI()
    {
        if (cardCountText == null) return;
        cardCountText.text = $"Running: {runningCount}\nTrue: {GetTrueCount():0.0}";
    }

    private void GenerateDeck()
    {
        deck.Clear();
        string[] suits = { "Spades", "Hearts", "Clubs", "Diamonds" };
        for (int d = 0; d < numerOfDecks; d++)
        {
            foreach (string suit in suits)
            {
                for (int i = 1; i <= 13; i++)
                {
                    int actualValue = (i == 1) ? 11 : (i >= 11 ? 10 : i);
                    var cardData = new CardData(suit, actualValue); // ensure CardData sets countValue
                    deck.Add(cardData);
                }
            }
        }
    }

    private void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int r = Random.Range(i, deck.Count);
            (deck[i], deck[r]) = (deck[r], deck[i]);
        }
    }

    public float GetTrueCount()
    {
        float decksRemaining = Mathf.Max(deck.Count / 52f, 1f);
        return runningCount / decksRemaining;
    }

    private void CheckShuffleNeeded()
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

    // Utility for clearing visuals
    public void ClearDealerArea()
    {
        ClearChildren(dealerArea);
    }
    public void ClearPlayerAreas()
    {
        foreach (var pa in playerAreas)
        {
            ClearChildren(pa.main);
            ClearChildren(pa.split);
        }
    }
    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }
}
