using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using static UnityEngine.Random;

[RequireComponent(typeof(ServerManager))]

public class GameManager : ServerCallbacks {

    public static GameManager Instance { get; private set; }

    public List<GatheringArea> totalGatheringAreasOnMap;
    public List<GameObject> avaiableNPCPrefabs;
    private List<Observable> obsList;
    public GameObject charContainer;
    private bool gameOn = false;
    private int[] scrambledIds;
    private bool amITheHost = false;
    public int totalNPCsPerPlayer = 2;
    // private int skinIdChosenByPlayer = 0; 
    private string playerName = ""; 
    public List<string> existingSkins = new List<string>();
    private int numberOfPlayersOnMatch;
    private List<string> allowedSkins = new List<string>();

    private NPCManager npcManager;
    private List<NetworkedPlayer> playersInfo;

    public int[] getScrambledIds()
    {
        return this.scrambledIds;
    }

    public List<GameObject> getAvaiableNPCPrefabs()
    {
        return this.avaiableNPCPrefabs;
    }

    public List<string> getAllowedSkins()
    {
        return this.allowedSkins;
    }

    public bool isAmITheHost()
    {
        return this.amITheHost;
    }

    public List<GatheringArea> getTotalGatheringAreasOnMap()
    {
        return this.totalGatheringAreasOnMap;
    }

    public NPCManager getNpcManager()
    {
        return this.npcManager;
    }

    void Start () {
        Application.runInBackground = true;
        this.npcManager = new NPCManager(totalNPCsPerPlayer, this.totalGatheringAreasOnMap, this.avaiableNPCPrefabs);
        this.applySettings();
        this.connectToServer();
        this.obsList = new List<Observable>();
    }
    private void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        Debug.Log("GameManager: OnEnable");
        Debug.Log("OnEnable called");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("GameManager: OnSceneLoaded: " + scene.name);
        Debug.Log("GameManager: " + mode);
    }

    public override void OnConnectionToServer(){
        print ("Connected to server");
    }
 
    public override void OnRegisterEvent(){
        print ("Game Session received from server");
        ServerManager.JoinOrCreateRoom(this.playerName, 0, 2);

    }
 
    public override void OnRoomJoin(Room room){
        print ("Joined room");
        print ("Maximum Players in the room :"+ room.maxPlayersPerRoom); 
        print ("Count of Players in the room :"+ room.roomMembers.Count);
        
    }

    public override void PlayerJoinedRoom(Room room){
        Debug.Log($"Player joined room {room}");
    }
 
    public override void OnRoomCreated(Room room){
        print ("Count of Players in the room :"+ room.roomMembers.Count);
        this.amITheHost = true;
        //If I created the room then im the host.
        //TODO: create the array of ids by reading the number of prefabs
        this.scrambledIds = (new int[] { 0, 1, 2, 3, 4, 5 }).OrderBy(a => Guid.NewGuid()).ToArray();
    }

    public override void OnNewObservableWithIdCreated(Observable observable){
        if (!this.amITheHost){
            Debug.Log($"Observable with id created: {observable}. Id is {observable.uniqueId}");
            this.obsList.Add(observable);
            NPC npc = observable.observeredTransform.gameObject.GetComponent<NPC>();
            npc.setUniqueId(observable.uniqueId);
            if(npc != null){
                this.npcManager.handleObservableNPC(npc);
            }            
        } 
    }
    public void npcCleanUp(NPC npcDestroyed)
    {

        Tuple<GatheringArea, GatheringAreaSlot> whereIsNPC = this.npcManager.whereIsNPC(npcDestroyed);
        if (whereIsNPC != null)
        {
            GatheringAreaSlot slot = whereIsNPC.Item2;
            if (slot != null)
            {
                slot.emptySlot();
            }
        }

        List<Observable> localList;
        if (!this.amITheHost)
        {
            localList = this.obsList;
        } else {
            localList= this.getObservablePlayer().observer.observables;
        }
        Observable obs = localList.First(x => x.observeredTransform == npcDestroyed.transform);
        if (!this.amITheHost) localList = obs.owner.observer.observables;
        localList.Remove(obs);
        Debug.Log("What to do?");
    }   
    public override void OnGameStart(){
        print ("Game Started");
        this.gameOn = true;     

        if (this.amITheHost)
        {
            this.loadAllPlayersInfo();

            this.npcManager.spawnAllNPCs();
        }
    }

    private void loadAllPlayersInfo(){
        this.playersInfo = ServerManager.currentRoomPlayers;

        foreach (NetworkedPlayer player in this.playersInfo) {
            Debug.Log($"player is {player}");
            this.allowedSkins.Add(player.GetComponent<Player>().getSkinId());
        }
    }

    public NetworkedPlayer getObservablePlayer(){
        return ServerManager.localPlayer;
    }

    public void applySettings(){
        
        ServerManager serverManager = this.GetComponent<ServerManager>();
        GameSettings settings = GameSettings.Instance;

        if (settings != null) {
            serverManager.hostIPAddress = settings.getIpAddress();
            serverManager.useLocalHostServer = true;
            //this.skinIdChosenByPlayer = settings.getSkinIdChosenByPlayer();
            this.playerName = settings.getPlayerName();
        } else {
            serverManager.hostIPAddress = "127.0.0.1";
        }
    }

    public string getSkinPrefabName(string skinId){
        switch (skinId)
        {
            case null: throw new ArgumentNullException(nameof(skinId));
            case "": throw new ArgumentException($"{nameof(skinId)} cannot be empty", nameof(skinId));
            default: return skinId.First().ToString().ToUpper() + skinId.Substring(1);
        }
    }

    public override void OnUDPEventReceived(GamePlayEvent gamePlayEvent) {
        Debug.Log($"GamePlayEvent received from server, event name: " + gamePlayEvent.eventName);
    }
 
}
