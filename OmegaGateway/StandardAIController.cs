using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class StandardAIController : AIController{


    //IDEA: calculate stuff and save it on boolean / state variables (the "state") so then you make the decisions in another module.
    // save "priorities"?

    private bool notEnoughFoodFactoriesForCurrentPops;
    private bool notEnoughFoodFactoriesForNextPops;
    private bool freeSpareLabor;

    private GalaxySector sectorToMove;

    public override void playTurn() {
        List<GalaxySystem> systems = GameManager.Instance.getAllSystemsOwnedBy(this.getPlayer());
        List<Ship> ships = GameManager.Instance.getAllActiveShipsOwnedBy(this.getPlayer());


        foreach (GalaxySystem system in systems){
            this.freeTheLabor(system);
            this.coverAllFood(system);
            this.maximizeProduction(system);
            this.buildOrders(system);
        }


        foreach (Ship ship in ships)
        {
            this.thinkStrategy(ship);
            this.act(ship);       
            this.sectorToMove = null;    
        }
    }
    private void freeTheLabor(GalaxySystem system){
        system.getDetails().freeAllLabor();
    }

    private void coverAllFood(GalaxySystem system){
            int foodSurplus = system.getDetails().getFoodSurplus();
            int freeLabor = system.getDetails().getCurrentFreeLabour();
            Debug.Log($"foodSurplus {foodSurplus}");
            Debug.Log($"freeLabor {freeLabor}");

            while(foodSurplus <= 0 || freeLabor == 0) {
                system.getDetails().assignLabourOnFactory(ProductionTypeEnum.FOOD, system.getDetails().getWorkingFactories(ProductionTypeEnum.FOOD) + 1);
                Debug.Log($"+1 FOOD");

                foodSurplus = system.getDetails().getFoodSurplus();
                freeLabor = system.getDetails().getCurrentFreeLabour();
                Debug.Log($"foodSurplus {foodSurplus}");
                Debug.Log($"freeLabor {freeLabor}");

            }; 

            this.notEnoughFoodFactoriesForCurrentPops = (freeLabor == 0 && foodSurplus < 0);
            int freeFoodFactories = system.getDetails().getMaxFoodFactories() - system.getDetails().getWorkingFactories(ProductionTypeEnum.FOOD);
            this.notEnoughFoodFactoriesForNextPops = freeFoodFactories <= 1;
            
    }
    private void maximizeProduction(GalaxySystem system){
        int freeLabor = system.getDetails().getCurrentFreeLabour();
        int freeIndustrySlots = system.getDetails().getMaxIndFactories() - system.getDetails().getWorkingFactories(ProductionTypeEnum.INDUSTRY);
        Debug.Log($"freeLabor {freeLabor}");
        Debug.Log($"freeIndustrySlots {freeIndustrySlots}");

        while (freeIndustrySlots > 0 || freeLabor != 0)
        {
            system.getDetails().assignLabourOnFactory(ProductionTypeEnum.INDUSTRY, system.getDetails().getWorkingFactories(ProductionTypeEnum.INDUSTRY) + 1);
            freeIndustrySlots = system.getDetails().getMaxIndFactories() - system.getDetails().getWorkingFactories(ProductionTypeEnum.INDUSTRY);
            freeLabor = system.getDetails().getCurrentFreeLabour();
            Debug.Log($"freeLabor {freeLabor}");
            Debug.Log($"freeIndustrySlots {freeIndustrySlots}");
        };

        this.freeSpareLabor = freeLabor > 0;
    }
    private void buildOrders(GalaxySystem system){
        if(system.getDetails().getBuildablesProductionQueue().Count == 0){
            if(this.notEnoughFoodFactoriesForCurrentPops) {
                Debug.Log($"system is starving and we are building a food factory.");
                system.getDetails().getBuildablesProductionQueue().Enqueue(BuildableManager.Instance.getBasicFactory(ProductionTypeEnum.FOOD));
            } else if(this.notEnoughFoodFactoriesForNextPops) {
                Debug.Log($"system could use another food factory.");
                system.getDetails().getBuildablesProductionQueue().Enqueue(BuildableManager.Instance.getBasicFactory(ProductionTypeEnum.FOOD));
            } else if(this.freeSpareLabor) {
                Debug.Log($"system has unemployment so we build to more industry.");
                system.getDetails().getBuildablesProductionQueue().Enqueue(BuildableManager.Instance.getBasicFactory(ProductionTypeEnum.INDUSTRY));
            } else {
                Debug.Log($"All good on system.building default ships");
                system.getDetails().getBuildablesProductionQueue().Enqueue(BuildableManager.Instance.getBuildableShipById("DESTROYER"));
            }
        } else {
            Buildable thingWeAreBuilding = system.getDetails().getBuildablesProductionQueue().Peek();
            if(this.notEnoughFoodFactoriesForCurrentPops && !thingWeAreBuilding.Equals(BuildableManager.Instance.getBasicFactory(ProductionTypeEnum.FOOD))){
                Debug.Log($"system is starving and we are building something different. Changing to food factory." );
                system.getDetails().getBuildablesProductionQueue().Clear();
                system.getDetails().getBuildablesProductionQueue().Enqueue(BuildableManager.Instance.getBasicFactory(ProductionTypeEnum.FOOD));
            } else {
                
            }            
        }
    }

    private void thinkStrategy(Ship ship){
        //For the time being just some random movement
        List<GalaxySector> availableSectors = ship.getCurrentSector().getNeighbours();
        this.sectorToMove = availableSectors.OrderBy(x => Guid.NewGuid()).FirstOrDefault();

    }

    private void act(Ship ship){
        
        if(this.sectorToMove != null){
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("location", this.sectorToMove.getCoordinate());
            ship.queueAction(new MoveAction(ship, parameters));
        }

    }
}
