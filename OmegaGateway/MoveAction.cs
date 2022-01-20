using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction: ShipAction
{

    public MoveAction(Ship ship, Dictionary<string, object> actionParameters)  {
        this.ship = ship;
        this.actionParameters = actionParameters;
    }

    public override void doAction(){
        Debug.Log($"About to do move action with these parameters: {this.actionParameters}");

        if(!this.actionParameters.ContainsKey("location")){
            Debug.LogError($"missing location, ship {ship} cant move");
        } else {
            object coord = this.actionParameters["location"];
            if(!(coord is GalaxyCoordinate)){
                Debug.LogError($"location {coord} not a galaxycoordinate, ship {ship} cant move");
            } else {
               ship.move((GalaxyCoordinate)coord);
            }
        }
        
    }

    


    

}