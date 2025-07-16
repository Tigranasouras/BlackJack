using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public string suit;
    public int value;
    public int countValue;

    public SpriteRenderer spriteRenderer; //To show the cards

    public Sprite[] cardSprites; //Assign 52 card Sprites in order or via Script

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
        //For example, a basic card sprite naming Strategy:
        //Names like "Spades_1", "Hearts_11", etc.
        string spriteName = $"{suit}_{value}";
        Sprite cardSprite = Resources.Load<Sprite>($"Cards/{spriteName}");

        if (cardSprite != null)
        {
            spriteRenderer.sprite = cardSprite;
        }
        else
        {
            Debug.LogWarning($"Missing sprite for {spriteName}");
        }
    }

}
