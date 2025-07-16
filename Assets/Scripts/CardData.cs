using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardData
{
    public string suit;
    public int value;
    public int countValue;

    public CardData(string suit, int value)
    {
        this.suit = suit;
        this.value = value;
        this.countValue = (value >= 2 && value <= 6) ? 1 :
                          (value >= 10 || value == 11) ? -1 : 0;
    }
}

