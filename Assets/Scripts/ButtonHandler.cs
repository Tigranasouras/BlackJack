using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonHandler : MonoBehaviour


{
    public GameManager gameManager;

    public void hitButton()
    {
        gameManager.onHit();
    }

    public void standButton()
    {
        gameManager.onStand();
    }
    public void doubleButton()
    {
        gameManager.onDouble();
    }

    public void nextHandButton()
    {
        gameManager.resetGame();
    }

    public void AddWagerButton1()
    {
        gameManager.addWager(1000);
    }
    public void AddWagerButton5()
    {
        gameManager.addWager(5000);
    }
    public void AddWagerButton10()
    {
        gameManager.addWager(10000);
    }

    public void AddWagerButtonAll()
    {
        gameManager.addWager(-15);
    }
    
    
}
