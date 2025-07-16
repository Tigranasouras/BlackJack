using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public string suit;
    public int value; // 1 = Ace, 11 = Jack, 12 = Queen, 13 = King
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
