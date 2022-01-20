A few of the projects that I have worked on my own with interesting code pieces:

1. Act Normal
2. Omega Gateway


# **_Act normal_**

Act normal was my first experiment with multiplayer: The idea was to achieve a mini version of the multiplayer PvP for Assassin's Creed: Revelations; where each player had a random different "skin", but in the field there were a bunch of NPCs with the same skins, so the gist was to hunt down another player without killing an NPC, while being hunted. Although the technical multiplayer side was achieved, I had some blockers getting a few game design aspects together, and also the character design was a challenge (Its still using placeholders). The project is on hold, although I would like to retake it again at some point.

Discleamer: The placerholders are mostly... ehm... "already owned" sprites. So keep that in mind.

<details>
  
  <summary>Screenshot</summary>
  
  ![alt text](https://github.com/EmilianoCenizo/public-code-porfolio/blob/master/img/an_screenshot_1.png "Act Normal screenshot 1")    
  
</details>

I developed a small NodeJS server whose only work is to send events in UDP. These events were coded in the clients (the Unity games), and included for example movement, actions and even game events like "game started" or "player left".

The client's behavior system design - although developed by me as well - was taken from some tutorials and code snippets online (so it wasn't 100% my idea). 
The connection opens a websocket for most in-game messages (sent with a "type"), but the most "noisy" information exchanges (like the position and rotation of "Observables" - NPCs and other players) are handled by USP. It also lets parties communicate with specific UDP messages. The messages are in JSON.


Interesting code pieces:

## GameManager
Orders most of the aspects of the game and acts as a liaison between the server behavior and the clients. It inherits from ServerCallbacks so it will receive all the notifications from events from the server.

### OnRoomCreated(Room room)
Creates the entity called "Room" so other players can connect. This also makes the player the "host", and sets the ids skins of the NPC ready.

### OnNewObservableWithIdCreated(Observable observable)
Handles and registers the creation of a new "Observable", that is, an object whose position and rotation are shared between all connected clients.

## ServerCallbacks
Contains the list of actions that a GameServer can subscribe to, and resolves its business behavior around it.

### ConnectToServer(string path = "")
Connects to a game server. It also registers the Action callbacks when these are received by `ServerManager`.

### OnDestroy()
Unsubscribes from all events.

## ServerManager
Module that handled most of the logic between the node server and the client. Its main job was to receive, handle, validate and send the requests back, as also transform them into different game events (like "gamestarted").

### Update()
On every frame the server checks if there are new messages from the server and resolves it.

### HandleWebSocketMessage(string message)
Gets a request from the websocket and creates a business logic request from it. The json message JSON must include a "type" that defines what the message is about. After "understanding" the message, its derived to the `ServerCallbacks` so it's handled by the business logic.

### CreateObservableObject(GamePlayEvent newObservableEvent)
When receiving a message of a new Observable created, it's registered so the server can receive new information about it - like position or rotation. 

### SyncObject(GamePlayEvent receivedEvent)
Updates all the transformation information of an registered observable.

### CreatePlayers(Player[] gamePlayers)
Spawns the players in the running client.

***

# **_Omega Gateway_**

Omega Gateway is the most structured game project - process wise -  I have worked so far. The game itself tries to emulate the 90s 4x sci fi games like Master of Orion 2; where you have a grid that represents galactic sectors, and there could be a system. Each system may have a player owned colony ("city"), and there they can play around city production attributes (food, industry, science, etc). Players can also build units (Ships) that can move around the map with certain restrictions. I'm still working on this project. 

<details>
  <summary>Screenshot 1 - Galaxy Map</summary>
  
  ![alt text](https://github.com/EmilianoCenizo/public-code-porfolio/blob/master/img/ow_screenshot_1.png "Omega Gateway - Map screenshot")    
  
</details>

<details>
  <summary>Screenshot 2 - Production screen</summary>
  
  ![alt text](https://github.com/EmilianoCenizo/public-code-porfolio/blob/master/img/ow_screenshot_2.png "Omega Gateway - Production screen screenshot")    

</details>


Interesting code pieces:

## SelectableEntity

Script to aggregate common behavior when selecting stuff from the map. Works in combination with Interface  `<ISelectable>`
that forces the object to implement the methods we will be consuming.

So, you only need to add the SelectableEntity script and implement the ISelectable interface to have complete click behavior for a click (run with `ISelectable.OnSelect`), a double click (run with `ISelectable.OnEnter`) and a right click (run with `ISelectable.OnAction`)

## GalaxyManger

It's function is to provide a high level access to behavior and the state of the map, including the sectors and the systems. It also builds the map.

### buildSectors()

Its job is to build the sectors on the map - and also assign them a system if it is supposed to.
It uses the this.usedMinimumValueForDensity attribute to generate random distributions for systems.

In the future the plan is to also take into account the relative position (so you can build "spiral" or "ring" types of maps for example).

### pathBetweenSectors(ShipsPresenceIndicator fleet, GalaxySector destination)
Takes a fleet and another sector and calculates the best path between them using the A* algorithm.
Each sector has a "cost" of navigating it (so it's avoided if possible), get with sectorMovementCost().
The sector also knows if the fleet has access to itself or not, on canFleetAccessSector(fleet)

## GalaxySector
Represents a specific "tile" in the map. It can hold a system, a space entity (like nebulas or pulsars), and/or a list of ships.

### sectorMovementCost()
Returns the cost of navigating that particular sector. For the time being is 1, but it will be affected by things like nebulas.

### canFleetAccessSector(ShipsPresenceIndicator fleet)
Returns true if the fleet has access to the sector. For the time being its just checking if the territory is owned by the player or not. In the future it will take other things into account, like the distance and the political stance between the players.

### shipLeftSector(Ship ship) | shipArrivedToSector(Ship ship)
Run every time a ship enters or exits the sector. Allows for additional checks.

### OnHover()
Run everytime you hover it with the mouse. If nothing is selected, it shows general information about itself. If there is a ship or fleet selected, draw the fastests path towards it using  `GalaxyManager.pathBetweenSectors()` by activating a "point" to mark it.

### OnAction()
On a right click on the sector, if there is a fleet or ship selected, movement actions are sent (see `ShipAction`)

## ShipAction
Ships need to handle several enqueued actions. So the abstract ShipAction class enables developing concrete action orders, consuming a  `Dictionary<string, object>` as a list of orders and executing them 1 by 1 after each turn.

## MoveAction
Basic type of action. The Dictionary holds the specific coordinate the ship is moving to.

### doAction()
Checks for the next "location" and moves the ship there after a turn. In the future it will take things like the ship speed into account.

## AIController
Abstract class that enables specific AI behaviors for players. Any player can hold a concrete class inherited from it. If it is null, then that means that the player is human. The GameManager iterates through every Player that holds a non-null AIController and executes the `playTurn()` before the new turn event is run.

## StandardAIController
First basic version of AI that ensures that the AI player doesn't starve, and prioritizes building ships and exploring randomly.

***
