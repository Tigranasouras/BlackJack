using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public string suit;
    public int value;
    public int countValue;

    public void SetCard(string suit, int value)
    {
        this.suit = suit;
        this.value = value;

        //Hi-Lo card counting framework
        if (value >= 2 && value <= 6)
        {
            countValue = 1;
        }
        else if (value >= 10 || value == 1)
        {
            countValue = -1;
        }
        else
        {
            countValue = 0;
        }

    }

}
