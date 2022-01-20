using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ShipAction
{

    protected Ship ship;

    protected Dictionary<string, object> actionParameters { get; set; }

    
    public abstract void doAction();


}
