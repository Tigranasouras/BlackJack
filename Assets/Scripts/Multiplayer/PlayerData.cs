using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public string playerName;
    public bool isBot;
    public bool isActive = true; // seat enabled (bots/humans). Inactive seats are ignored.
    public int cash;
    public int wager;
    public List<Card> hand = new List<Card>();
    public bool isDone;
    public List<Card> splitHand = new List<Card>();
    public bool hasSplit = false;
    public bool playingSplit = false;
    public int splitWager = 0;

    public PlayerData(string name, bool bot, int startingCash)
    {
        playerName = name;
        isBot = bot;
        cash = startingCash;
    }
}