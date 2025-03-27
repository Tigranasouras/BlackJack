using System.Collections;
using System.Collections.Generic;
using TMPro.Examples;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int playerScore = 0;
    public int dealerScore = 0;
    public TMPro.TextMeshProUGUI statusText;

    public void onHit()
    {
        //Deal Card
        playerScore += 0; // Score of dealt card

        if (playerScore > 21)
        {
            statusText.text = "You busted!";
        }
    }

    public void onStand()
    {
        while (dealerScore < 17)
        {
            //DealCard
            dealerScore += 0; //The score of the dealt card
        }
        if (dealerScore > 21 || playerScore > dealerScore)
        {
            statusText.text = "You win!";
        }
        else if (playerScore < dealerScore)
        {
            statusText.text = "Dealer wins";

        }
        else
        {
            statusText.text = "Push!";

        }
        // What happens next


    }


    int DrawRandomCardScore()
    {
        return Random.Range(1, 11); // Random card dealing for testing
    }

}
