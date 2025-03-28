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
}
