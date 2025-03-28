using System.Collections;
using System.Collections.Generic;
using TMPro.Examples;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int playerScore = 0;
    public int dealerScore = 0;
    public TMPro.TextMeshProUGUI statusText;
    public CardManager cardManager;
    public TMPro.TextMeshProUGUI dealerCountText;
    public TMPro.TextMeshProUGUI playerCountText;

    public void onHit()
    {
        cardManager.DealCard(true);
        playerScore += DrawRandomCardScore(); // Score of dealt card

        playerCountText.text = "Player Count: " + playerScore.ToString("00");


        if (playerScore > 21)
        {
            statusText.text = "You busted!";
        }
    }

    public void onStand()
    {

        while (dealerScore < 17)
        {
            cardManager.DealCard(false);
            dealerScore += DrawRandomCardScore(); //The score of the dealt card

            dealerCountText.text = "Dealer Count: " + dealerScore.ToString("00");

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
