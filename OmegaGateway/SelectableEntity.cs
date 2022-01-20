using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ISelectable))]
public class SelectableEntity : MonoBehaviour 
{
    private ISelectable selectableObject;
    private float clicktime = 0;
    private float clickdelay = 0.5f;
    private float clicked = 0;

    void Start(){
        this.selectableObject = this.GetComponent<ISelectable>();
    }

    void OnMouseOver()
    {
        if (!EventSystem.current.IsPointerOverGameObject()) 
        {
            if (Input.GetMouseButtonDown(0)){
                if ( clicked < 1 || Time.time - clicktime > clickdelay) { //single click
                    clicked++;
                    if (clicked >= 1) clicktime = Time.time;
                    Debug.Log($"Left clicked on {this.selectableObject}");
                    this.selectableObject.OnSelect();
                } else {
                    clicked = 0;
                    clicktime = 0;
                    Debug.Log($"Double clicked on {this.selectableObject}");
                    this.selectableObject.OnEnter();
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                Debug.Log($"Right clicked on {this.selectableObject}");
                selectableObject.OnAction();
            }
        
        }
    }
}
