using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerBiuttonHandler : MonoBehaviour


{
    public MultiplayerGameManager multiplayerGameManager;

    public void hitMButton()
    {
        multiplayerGameManager.OnHit();
    }

    public void standmButton()
    {
        multiplayerGameManager.OnStand();
    }


    //Make Double



    //make Split

    //make reset?


    //Add Wagering



}
