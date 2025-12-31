using System;
using System.Collections.Generic;

// DTOs for syncing the blackjack table state over Steam P2P.
// JsonUtility-friendly: fields + [Serializable].

[Serializable]
public class BJCardDTO
{
    public string suit;
    public int value;
    public bool faceUp;
}

[Serializable]
public class BJPlayerDTO
{
    public string playerName;
    public bool isBot;

    public int cash;
    public int wager;
    public int splitWager;

    public bool hasSplit;
    public bool playingSplit;
    public bool isDone;

    public List<BJCardDTO> hand = new();
    public List<BJCardDTO> splitHand = new();
}

[Serializable]
public class BJTableDTO
{
    public int currentPlayerIndex;
    public bool roundInProgress;

    public List<BJPlayerDTO> players = new();
    public List<BJCardDTO> dealerHand = new();

    // Optional, purely cosmetic
    public string statusText;
}

[Serializable]
public class BJNetEnvelope
{
    public string type;          // e.g. "REQ_HIT", "STATE"
    public ulong senderSteamId;
    public int seatIndex;
    public int amount;

    public BJTableDTO state;
}
