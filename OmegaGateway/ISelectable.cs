using UnityEngine;

public interface ISelectable {

    void OnSelect();

    void OnAction();
    void OnEnter();

    GameObject getSelectionBox();

}