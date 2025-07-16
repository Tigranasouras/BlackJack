using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public string suit;
    public int value;   // 1 = Ace, 11 = Jack, etc. (for sprite lookup)
    public int realValue;  // Blackjack logic value: 2–10, 10 (for J/Q/K), 11 (for Aces)
    public int countValue;
    bool show;

    public Image image; //To show the cards
    private Sprite frontSprite; //Stores the correct face of the card after SetCard()
    public Sprite backSprite; //assigned in the Inspector

    private bool isFaceUp = true;


    public void SetCard(string suit, int value)
    {
        this.suit = suit;
        this.value = value;

        //Assign BlackJack value
        if (value == 1)
        {
            realValue = 11; //Ace
        }
        else if (value >=11 && value <= 13)
        {
            realValue = 10; // J,Q, K
        }
        else
        {
            realValue = value; //2-10
        }

        //Hi-Lo Card COunting
        if (value >= 2 && value <= 6)
        {
            countValue = 1;
        }
        else if (realValue >= 10)
        {
            countValue = -1;
        }
        else
        {
            countValue = 0;
        }

        UpdateCardVisual();

    }

    private void UpdateCardVisual()
    {
        string path = $"Cards/{suit}_{value}"; // E.g., "Cards/Spades_1"
        Sprite cardSprite = Resources.Load<Sprite>(path);

        if (cardSprite != null)
        {
            frontSprite = cardSprite;
            image.sprite = frontSprite;
        }
        else
        {
            Debug.LogWarning($"Missing sprite for {path}");
        }
    }

    public void ShowBack(bool show)
    {
        isFaceUp = !show;
        image.sprite = show ? backSprite : frontSprite;
    }

    public bool IsFaceUp()
    {
        return isFaceUp;
    }


}
