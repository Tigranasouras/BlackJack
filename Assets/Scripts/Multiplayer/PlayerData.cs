using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerData : MonoBehaviour
{
    public string playerName;
    public bool isBot;
    public int cash;
    public int wager;
    public List<Card> hand = new List<Card>();
    public bool isDone;

    public PlayerData(string name, bool bot, int startingCash)
    {
        playerName = name;
        isBot = bot;
        cash = startingCash;
    }
}
