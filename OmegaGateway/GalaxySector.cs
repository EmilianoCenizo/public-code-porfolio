using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class GalaxySector : MonoBehaviour, ISelectable
{

    private GalaxySystem system;
    public GalaxySector sectorNorth;
    public GalaxySector sectorSouth;
    public GalaxySector sectorEast;
    public GalaxySector sectorNorthEast;
    public GalaxySector sectorSouthEast;
    public GalaxySector sectorWest;
    public GalaxySector sectorNorthWest;
    public GalaxySector sectorSouthWest;

    [SerializeField]
    private GameObject shipPresenceIcon;

    private GalaxyCoordinate coordinate;
    [SerializeField]
    private GameObject movementIndicator;

    private GalaxyManager manager;

    private ShipsPresenceIndicator indicator;

    private Player owner;

    void Start(){
        GameManager.Instance.nextTurnEvent += this.OnNextTurn;
        this.indicator = this.shipPresenceIcon.GetComponent<ShipsPresenceIndicator>();
    }
    public Player getOwner()
    {
        return this.owner;
    }

    public void setOwner(Player owner)
    {
        this.owner = owner;
    }
    public void setManager(GalaxyManager manager)
    {
        this.manager = manager;
    }

    private void OnNextTurn(int newTurn)
    {
        this.handleShipCheck();
    }

    public GalaxyCoordinate getCoordinate()
    {
        return this.coordinate;
    }

    public void setCoordinate(GalaxyCoordinate coordinate)
    {
        this.coordinate = coordinate;
    }

    public GalaxySystem getSystem()
    {
        return this.system;
    }

    public GalaxySector getSectorNorth()
    {
        return this.sectorNorth;
    }

    public void setSectorNorth(GalaxySector sectorNorth)
    {
        this.sectorNorth = sectorNorth;
    }

    public GalaxySector getSectorSouth()
    {
        return this.sectorSouth;
    }

    public void setSectorSouth(GalaxySector sectorSouth)
    {
        this.sectorSouth = sectorSouth;
    }
    public GalaxySector getSectorEast()
    {
        return this.sectorEast;
    }

    public void setSectorEast(GalaxySector sectorEast)
    {
        this.sectorEast = sectorEast;
    }
    public GalaxySector getSectorWest()
    {
        return this.sectorWest;
    }

    public void setSectorWest(GalaxySector sectorWest)
    {
        this.sectorWest = sectorWest;
    }
    public void setSystem(GalaxySystem system)
    {
        this.system = system;
    }
    public GalaxySector getSectorNorthEast()
    {
        return this.sectorNorthEast;
    }

    public void setSectorNorthEast(GalaxySector sectorNorthEast)
    {
        this.sectorNorthEast = sectorNorthEast;
    }


    public GalaxySector getSectorSouthEast()
    {
        return this.sectorSouthEast;
    }

    public void setSectorSouthEast(GalaxySector sectorSouthEast)
    {
        this.sectorSouthEast = sectorSouthEast;
    }

    public GalaxySector getSectorNorthWest()
    {
        return this.sectorNorthWest;
    }

    public void setSectorNorthWest(GalaxySector sectorNorthWest)
    {
        this.sectorNorthWest = sectorNorthWest;
    }
    public GalaxySector getSectorSouthWest()
    {
        return this.sectorSouthWest;
    }

    public void setSectorSouthWest(GalaxySector sectorSouthWest)
    {
        this.sectorSouthWest = sectorSouthWest;
    }

    public List<Ship> getCurrentShipsInSector()
    {
        return this.indicator.getShips();
    }

    void OnMouseEnter()
    {  
        this.manager.clearSectorPaths();
        this.OnHover();
    }

    public override string ToString() {
        return $"Sector {this.coordinate}";
    }
    
    public void addShip(Ship ship){
        
        ship.transform.parent = this.transform;
        ship.setCurrentSector(this);
        this.indicator.getShips().Add(ship.GetComponent<Ship>());
        this.handleShipCheck();
    }
    public void OnSelect()
    {
        GameManager.Instance.unselectItem();
        if (this.indicator != null && this.indicator.gameObject.activeSelf) UIManager.Instance.showSelectionBox(this.indicator.getSelectionBox());
        if (this.system != null) UIManager.Instance.showSelectionBox(this.system.getSelectionBox(), false); 
    }
    public void OnAction()
    {
        ISelectable selectedItem = GameManager.Instance.getSelectedItem();
        if (selectedItem != null)
        {
            if (selectedItem is Ship)
            {
                Ship selectedShip = (Ship)selectedItem;
            }
            else if (selectedItem is ShipsPresenceIndicator)
            {
                ShipsPresenceIndicator selectedFleet = (ShipsPresenceIndicator)selectedItem;
                this.handleSelectedFleetAction(selectedFleet);
            }
            else
            {
                Debug.Log($"{selectedItem} is not a ship nor a fleet");
            }
        }
        this.manager.clearSectorPaths();
        GameManager.Instance.unselectItem();
    }
    public void OnEnter()
    {

    }

    public void OnHover()
    {   
        UIManager.Instance.setInfoInHoverBox(this.ToString());
        ISelectable selectedItem = GameManager.Instance.getSelectedItem();
        if(selectedItem != null){
            if(selectedItem is Ship){
                Ship selectedShip = (Ship)selectedItem;
            } else if(selectedItem is ShipsPresenceIndicator){
                ShipsPresenceIndicator selectedFleet = (ShipsPresenceIndicator)selectedItem;
                this.handleSelectedFleetHover(selectedFleet);
            } else {
                // Debug.Log($"{selectedItem} is not a ship nor a fleet");
            }
        }
    }
    private void handleSelectedFleetAction(ShipsPresenceIndicator fleet){
        List<GalaxyCoordinate> path = this.manager.pathBetweenSectors(fleet, this);
        if(path != null) {
            fleet.handleFleetMovement(this, path);
        } else {
            Debug.Log($"No path from {fleet} to {this}");
        }
    }

    private void handleSelectedFleetHover(ShipsPresenceIndicator fleet){
        // Debug.Log($"DISTANCE FROM SECTOR WHERE FEEL IS TO HERE: {this.distanceToSector(fleet.getCurrentSector())}");
        List<GalaxyCoordinate> pathBetweenSectors = this.manager.pathBetweenSectors(fleet, this);
        if(pathBetweenSectors != null){
            pathBetweenSectors.ForEach(sector => this.manager.getSector(sector).drawPath());
        }
        
    }

    public void drawPath(){
        this.movementIndicator.SetActive(true);
    }

    public void clearPath(){
        this.movementIndicator.SetActive(false);
    }

    public GameObject getSelectionBox()
    {
        return null;
    }

    void Update(){

    }

    public void highlightItemAsSelected(){
        //TODO    
    }

    private void handleShipCheck(){
        this.indicator.handleShipsIcons();
    }

    public int distanceToSector(GalaxySector otherSector){
        GalaxyCoordinate otherSectorCoordinates = otherSector.getCoordinate();

        return Math.Max(Math.Abs(this.getCoordinate().getRow() - otherSectorCoordinates.getRow()), 
        Math.Abs(this.getCoordinate().getColumn() - otherSectorCoordinates.getColumn()));

    }

    public int sectorMovementCost(){
        return 1;
    }

    public List<GalaxySector> getNeighbours(){
        List<GalaxySector> neighbours = new List<GalaxySector>();
        if(this.sectorNorth != null) neighbours.Add(sectorNorth);
        if(this.sectorNorthEast != null) neighbours.Add(sectorNorthEast);
        if(this.sectorEast != null) neighbours.Add(sectorEast);
        if(this.sectorSouthEast != null) neighbours.Add(sectorSouthEast);
        if(this.sectorSouth != null) neighbours.Add(sectorSouth);
        if(this.sectorSouthWest != null) neighbours.Add(sectorSouthWest);
        if(this.sectorWest != null) neighbours.Add(sectorWest);
        if(this.sectorNorthWest != null) neighbours.Add(sectorNorthWest);

        return neighbours;
    }

    void OnMouseExit()
    {
        this.OnUnhover();
    }

    public bool canFleetAccessSector(ShipsPresenceIndicator fleet) {      
        return (this.owner == null || this.owner == fleet.getShips()[0].getOwner());
    }

    public void OnUnhover()
    {
        UIManager.Instance.removeFromHoverBox();
        this.clearPath();
    }

    public void shipLeftSector(Ship ship){
        this.getCurrentShipsInSector().Remove(ship);
        this.handleShipCheck();
    }
    public void shipArrivedToSector(Ship ship){
        this.getCurrentShipsInSector().Add(ship);
        this.handleShipCheck();
    }
}
