using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIController {

    private Player aiPlayer;

    public Player getPlayer(){
        return this.aiPlayer;
    }

    public void setPlayer(Player player){
        this.aiPlayer = player;
    }

    public abstract void playTurn();

}
