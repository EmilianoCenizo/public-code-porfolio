using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GalaxyManager : MonoBehaviour
{
    [SerializeField]
    private int seed = 0;

    [SerializeField]
    private int size = 10;

    [SerializeField]
    private GameObject gridContainer;
    [SerializeField]
    private GameObject gridPrefab;

    [SerializeField]
    private GameObject starSystemPrefab;
    private Dictionary<GalaxyCoordinate,GalaxySector> sectors = new Dictionary<GalaxyCoordinate, GalaxySector>(new GalaxyCoordinateComparer());
 
    public float highSystemsDensityMinValue = -2f;
    public float mediumSystemsDensityMinValue = -5f;
    public float lowSystemsDensityMinValue = -8f;
    
    private float usedMinimumValueForDensity = -1f;
    public SystemDensityEnum systemDensity = SystemDensityEnum.HIGH;

    public void createNewGalaxy() {

        this.applyGameParameters();
        this.buildSectors();

    }
    public int getSize()
    {
        return this.size;
    }

    private void applyGameParameters(){
        if (this.seed != 0)
        {
            Random.InitState(this.seed);
        }

        switch (this.systemDensity)
        {
            case SystemDensityEnum.HIGH:
                this.usedMinimumValueForDensity = this.highSystemsDensityMinValue;
                break;
            case SystemDensityEnum.MEDIUM:
                this.usedMinimumValueForDensity = this.mediumSystemsDensityMinValue;
                break;
            case SystemDensityEnum.LOW:
                this.usedMinimumValueForDensity = this.lowSystemsDensityMinValue;
                break;
            default:
                this.usedMinimumValueForDensity = this.highSystemsDensityMinValue;
                break;
        }
    }

    private void buildSectors(){

        for (int i = 65; i < this.size+66; i++) {
            for (int j = 1; j < this.size+1; j++) {
                
                GalaxyCoordinate coordinate = new GalaxyCoordinate((char)i,j);
                Debug.Log($"Created coordinate {coordinate}");

                GameObject instanciateGrid = Instantiate(this.gridPrefab, this.calculateCoordinatePosition(coordinate), Quaternion.identity, this.gridContainer.transform);
                
                GalaxySector sector = instanciateGrid.GetComponent<GalaxySector>();
                sector.setCoordinate(coordinate);
                sector.setManager(this);
                instanciateGrid.name = $"Grid {coordinate.ToString()}";

                float systemPresentCalculation = Random.Range(this.usedMinimumValueForDensity, 1f);
                //TODO: Something to verify if the system count is too low (or too high?)

                if(systemPresentCalculation > 0) {
                    this.buildSystem(sector);
                }

                this.sectors.Add(coordinate, sector);

            }
        }

        this.setNeighbours();
    }

    private void setNeighbours(){
        foreach (var sector in this.sectors)
        {
            GalaxyCoordinate north = sector.Key.getNorthCoordinate();
            GalaxyCoordinate south = sector.Key.getSouthCoordinate();
            GalaxyCoordinate east = sector.Key.getEastCoordinate();
            GalaxyCoordinate west = sector.Key.getWestCoordinate();
            GalaxyCoordinate northEast = sector.Key.getNorthEastCoordinate();
            GalaxyCoordinate southEast = sector.Key.getSouthEastCoordinate();
            GalaxyCoordinate northWest = sector.Key.getNorthWestCoordinate();
            GalaxyCoordinate southWest = sector.Key.getSouthWestCoordinate();

            if(north != null){
                sector.Value.setSectorNorth(sectors[north]);
            }
            if(south != null){
                sector.Value.setSectorSouth(sectors[south]);
            }
            if(east != null){
                sector.Value.setSectorEast(sectors[east]);
            }
            if(west != null){
                sector.Value.setSectorWest(sectors[west]);
            }
            if(northEast != null){
                sector.Value.setSectorNorthEast(sectors[northEast]);
            }
            if(southEast != null){
                sector.Value.setSectorSouthEast(sectors[southEast]);
            }
            if(northWest != null){
                sector.Value.setSectorNorthWest(sectors[northWest]);
            }
            if(southWest != null){
                sector.Value.setSectorSouthWest(sectors[southWest]);
            }
        }
    }

    private void buildSystem(GalaxySector sector) {
        GameObject starSystem = Instantiate(this.starSystemPrefab, Vector3.zero, Quaternion.identity, sector.transform);
        GalaxySystem system = starSystem.GetComponent<GalaxySystem>();
        system.setSector(sector);
        sector.setSystem(system);
        Debug.Log($"System {system.getSystemName()} built on sector {sector.getCoordinate().ToString()}");
        GameManager.Instance.addSystemToList(system);

    }

    private Vector3 calculateCoordinatePosition(GalaxyCoordinate coordinate){
        
        int columnPosition = coordinate.getColumn() - 65;
        int rowPosition = coordinate.getRow();

        return new Vector3(columnPosition,-rowPosition,1);
    }

    public GalaxySector getSector(GalaxyCoordinate coordinate){
        return this.sectors[coordinate];
    } 

    public void clearSectorPaths(){
        foreach (var sector in this.sectors)
        {
            sector.Value.clearPath();
        }
    }

    public List<GalaxyCoordinate> pathBetweenSectors(ShipsPresenceIndicator fleet, GalaxySector destination){
      
        List<GalaxyCoordinate> activeList = new List<GalaxyCoordinate>();
        List<GalaxyCoordinate> visitedList = new List<GalaxyCoordinate>();
        GalaxyCoordinate startingSector = fleet.getCurrentSector().getCoordinate();
        activeList.Add(startingSector);

        while (activeList.Any()){

            GalaxyCoordinate sectorToCheck = activeList.OrderBy(sector => sector.getPathingNode().CostDistance).First();

            if(sectorToCheck.Equals(destination.getCoordinate())) {
                Debug.Log("Reached position");
                List<GalaxyCoordinate> coords = sectorToCheck.getPathingNode().fullPath();
                coords.Reverse();
                return coords;
            } else {

                visitedList.Add(sectorToCheck);
                activeList.Remove(sectorToCheck);

                List<GalaxyCoordinate> walkableSectors = this.getPossibleNeighboursToWalk(fleet, sectorToCheck, destination);

                foreach (GalaxyCoordinate walkableSector in walkableSectors) {
                    if (visitedList.Any(x => x.Equals(walkableSector))){
                        continue;
                    }
                    if (activeList.Any(x => x.Equals(walkableSector)))
                    {
                        GalaxyCoordinate existingSector = activeList.First(x => x.Equals(walkableSector));
                        if (existingSector.getPathingNode().CostDistance > sectorToCheck.getPathingNode().CostDistance ){
                            activeList.Remove(existingSector);
                            activeList.Add(walkableSector);
                        }
                    } else {
                        activeList.Add(walkableSector);
                    }
                }
            }
        }
        Debug.Log("No path!");
        return null;
    }

    private List<GalaxyCoordinate> getPossibleNeighboursToWalk(ShipsPresenceIndicator fleet, GalaxyCoordinate sectorToCheck, GalaxySector destination){
        
        List<GalaxyCoordinate> neighbours = sectorToCheck.getAllNeighbours();
                    
        foreach (GalaxyCoordinate neighbour in neighbours) {
            neighbour.pathingNode.Cost = this.sectors[neighbour].sectorMovementCost() + sectorToCheck.getPathingNode().Cost;
            neighbour.pathingNode.Parent = (GalaxyCoordinate)sectorToCheck;
        }
        neighbours.ForEach(neighbour => neighbour.getPathingNode().SetDistance(destination.getCoordinate()));
       
        return neighbours.Where(sector => this.sectors[sector].canFleetAccessSector(fleet)).ToList();
    }


    // Update is called once per frame
    void Update()
    {
           
    }
}
