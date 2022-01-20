using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Threading.Tasks;

public class ServerManager : MonoBehaviour
{
    static WebSocketClient wsClient;
    static UDPClient udpClient;
    public static Session gameSession;
    public static List<NetworkedPlayer> currentRoomPlayers = new List<NetworkedPlayer>();
    public static NetworkedPlayer MessageSender;
    public static NetworkedPlayer localPlayer;
    public static bool gameStarted;
    public static List<Observable> observers = new List<Observable>();
    public const int defaultServerUDPPort = 5000;
    public const int defaultServerTCPPort = 3000;
    public Transform WorldOriginTransform;
    byte[] udpMsg;
    string wsMsg;
    public List<NetworkedPlayer> SpawnPrefabs;
    public List<PositionAndRotationEulers> SpawnInfo;
    public bool useLocalHostServer;
    public string hostIPAddress;
    private static ServerManager instance;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (wsClient == null)
        {
            return;
        }
        //websocket receive queue
        var ws_queue = wsClient.receiveQueue;
        while (ws_queue.TryPeek(out wsMsg))
        {
            ws_queue.TryDequeue(out wsMsg);
            HandleWebSocketMessage(wsMsg);
            wsMsg = null;
        }
        if (udpClient == null)
        {
            return;
        }
        //udp receive queue
        while (udpClient.receiveQueue.TryPeek(out udpMsg))
        {
            udpClient.receiveQueue.TryDequeue(out udpMsg);
            HandleUDPMessage(Encoding.UTF8.GetString(udpMsg));
            udpMsg = null;
        }
    }

    void OnDestroy()
    {
        observers = null;
        currentRoomPlayers = null;
        if (wsClient != null)
        {
            wsClient.tokenSource.Cancel();
        }
        if (udpClient != null)
        {
            udpClient.Dispose();
        }
        if (Application.platform == RuntimePlatform.OSXPlayer
            || Application.platform == RuntimePlatform.WindowsPlayer
            || Application.platform == RuntimePlatform.LinuxPlayer)
        {
            Environment.Exit(0);
        }
    }

    public async Task ConnectToServer(string path = "")
    {
        try
        {
            gameSession = new Session(); 
            wsClient = new WebSocketClient();
            Uri uri = new Uri("ws://" + hostIPAddress + ":" + defaultServerTCPPort + path);
            if (wsClient.isOpen())
            {
                wsClient.Dispose();
                wsClient = new WebSocketClient();
            }
            await wsClient.Connect(@uri);
            ServerCallbacks.connectedToServer();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect to your Local Host");
        }
    }

    public static NetworkedPlayer GetPlayer(string playerId)
    {
        return currentRoomPlayers.Find(player => player.playerId == playerId);
    }
    public static NetworkedPlayer GetPlayer(int playerIndex)
    {
        return currentRoomPlayers.Find(player => player.playerIndex == playerIndex);
    }
    public static void JoinOrCreateRoom(string playerName, int playerAvatar, int maxPlayersPerRoom, Dictionary<string,string> playerTags = null)
    {
        Hashtable playertagsHashtable;
        if (playerTags != null)
        {
            playertagsHashtable = Tag.DictionaryToHashtable(playerTags);
        }
        else
        {
            playertagsHashtable = null;
        }
        JoinOrCreateRoomRequest createOrJoinRoomRequest = new JoinOrCreateRoomRequest(playerName, playerAvatar, maxPlayersPerRoom, playertagsHashtable);
        wsClient.Send(Messaging<JoinOrCreateRoomRequest>.Serialize(createOrJoinRoomRequest));
    }

    public static void GetRooms()
    {
        GetRoomsRequest getRoomsRequest = new GetRoomsRequest();
        wsClient.Send(Messaging<GetRoomsRequest>.Serialize(getRoomsRequest));
    }

    public static void GetAvailableRooms()
    {
        GetAvailableRoomsRequest getAvailableRoomsRequest = new GetAvailableRoomsRequest();
        wsClient.Send(Messaging<GetAvailableRoomsRequest>.Serialize(getAvailableRoomsRequest));
    }
    public static void CreateRoom(string playerName, int playerAvatar, int maxPlayersPerRoom, Dictionary<string, string> playerTags = null)
    {
        if (gameSession.roomId == "")
        {
            Hashtable playertagsHashtable;
            if (playerTags != null)
            {
                playertagsHashtable = Tag.DictionaryToHashtable(playerTags);
            }
            else
            {
                playertagsHashtable = null;
            }
            CreateRoomRequest createRoomRequest = new CreateRoomRequest(playerName, playerAvatar, maxPlayersPerRoom, playertagsHashtable);
            wsClient.Send(Messaging<CreateRoomRequest>.Serialize(createRoomRequest));
        }
        else
        {
            Debug.LogError("Player is already a member in another room");
        }
    }

    public static void JoinRoom(string roomId, string playerName, int playerAvatar, Dictionary<string, string> playerTags = null)
    {
        if (gameSession.roomId == "")
        {
            Hashtable playertagsHashtable;
            if (playerTags != null)
            {
                playertagsHashtable = Tag.DictionaryToHashtable(playerTags);
            }
            else
            {
                playertagsHashtable = null;
            }
            JoinRoomRequest joinRoomRequest = new JoinRoomRequest(roomId, playerName, playerAvatar, playertagsHashtable);
            wsClient.Send(Messaging<JoinRoomRequest>.Serialize(joinRoomRequest));
        }
        else
        {
            if (gameSession.roomId == roomId)
            {
                Debug.LogError("Player is already a member in this room");
            }
            else
            {
                Debug.LogError("Player is already a member in another room");
            }
        }
    }

    public static void SendUDPMessage(GamePlayEvent gameplayEvent)
    {
        gameplayEvent.roomId = gameSession.roomId;
        gameplayEvent.senderId = gameSession.playerId;
        if (udpClient.run)
        {
            udpClient.Send(gameplayEvent.ToJson());
        }
        else
        {
            Debug.LogError("Error in sending UDP Message");
        }
    }

    public static void Disconnect()
    {
        wsClient = null;
        udpClient = null;
        foreach (NetworkedPlayer player in currentRoomPlayers)
        {
            Destroy(player.gameObject);
        }
        currentRoomPlayers = new List<NetworkedPlayer>();
        gameSession = new Session();
        gameStarted = false;
        MessageSender = null;
        localPlayer = null;
        observers = new List<Observable>();
    }

    public static void ExitRoom()
    {
        ExitRoomRequest exitRoomRequest = new ExitRoomRequest();
        wsClient.Send(Messaging<ExitRoomRequest>.Serialize(exitRoomRequest));
    }


    public void SendGamePlayEvent(GamePlayEvent serverEvent)
    {
        serverEvent.roomId = gameSession.roomId;
        serverEvent.senderId = gameSession.playerId;
        wsClient.Send(Messaging<GamePlayEvent>.Serialize(serverEvent));
    }

    void HandleWebSocketMessage(string message)
    {
        var msg = MessageWrapper.UnWrapMessage(message);
        switch (msg.type)
        {
            case "register":
                Register register = Messaging<Register>.Deserialize(message);
                gameSession.sessionId = register.sessionId;
                gameSession.playerId = register.playerId;
                ServerCallbacks.registerEvent();
                break;

            case "notification":
                Notification notification = Messaging<Notification>.Deserialize(message);
                switch (notification.notificationText)
                {
                    case "left-room":
                        gameSession.roomId = "";
                        ServerCallbacks.leftRoom();
                        break;
                    case "join-room-faliure":
                        ServerCallbacks.joinRoomFaliure();
                        break;
                    case "new-room-created-in-lobby":
                        ServerCallbacks.newRoomCreatedInLobby();
                        break;
                    case "room-removed-from-lobby":
                        ServerCallbacks.roomRemovedFromLobby();
                        break;
                }
                ServerCallbacks.notificationEvent(notification);
                break;

            case "roomsList":
                RoomsList roomsList = Messaging<RoomsList>.Deserialize(message);
                ServerCallbacks.roomsList(roomsList.rooms);
                break;

            case "roomCreated":
                RoomCreated roomCreated = Messaging<RoomCreated>.Deserialize(message);
                gameSession.roomId = roomCreated.room.roomId;
                ServerCallbacks.roomCreated(roomCreated.room);
                break;

            case "roomJoin":
                RoomJoin roomJoin = Messaging<RoomJoin>.Deserialize(message);
                gameSession.roomId = roomJoin.room.roomId;
                ServerCallbacks.roomJoin(roomJoin.room);
                break;

            case "playerJoinedRoom":
                PlayerJoinedRoom playerJoinedRoom = Messaging<PlayerJoinedRoom>.Deserialize(message);
                ServerCallbacks.playerRoomJoined(playerJoinedRoom.room);
                break;

            case "gameStart":
                GameStart gameStart = Messaging<GameStart>.Deserialize(message);
                gameSession.currentPlayers = gameStart.room.roomMembers.ToArray();
                foreach (Player player in gameStart.room.roomMembers)
                {
                    if (player.playerId == gameSession.playerId)
                    {
                        gameSession.playerIndex = player.playerIndex;
                    }
                }
                CreatePlayers(gameSession.currentPlayers);
                gameStarted = true;
                ServerCallbacks.gameStart();

                udpClient = new UDPClient(hostIPAddress, defaultServerUDPPort);
                SendUDPMessage(new GamePlayEvent(){eventName = "Start"});

                break;

            case "GamePlayEvent":
                GamePlayEvent gamePlayEvent = Messaging<GamePlayEvent>.Deserialize(message);
                switch (gamePlayEvent.eventName)
                {
                    case "NewObservableCreated":
                        CreateObservableObject(gamePlayEvent);
                        break;
                    case "NewObservableWithIdCreated":
                        CreateObservableWithIdObject(gamePlayEvent);
                        break;
                    default:
                        ReflectEvent(gamePlayEvent);
                        break;
                }
                break;

            case "memberLeft":
                RoomMemberLeft playerLeft = Messaging<RoomMemberLeft>.Deserialize(message);
                ServerCallbacks.playerLeft(playerLeft);
                break;

            default:
                Debug.LogError("Unknown WebSocket message arrived: " + msg.type + ", message: " + message);
                break;
        }
    }

    void HandleUDPMessage(string message)
    {
        GamePlayEvent gamePlayEvent = JsonUtility.FromJson<GamePlayEvent>(message);
        switch (gamePlayEvent.type)
        {
            case "GamePlayEvent":
                if (gamePlayEvent.eventName == "Observable")
                {
                    SyncObject(gamePlayEvent);
                }
                else
                {
                    ServerCallbacks.udpEventReceived(gamePlayEvent);
                }
                break;
            default:
                Debug.LogError("Unknown UDP message arrived: " + message);
                break;
        }
    }

    void CreateObservableObject(GamePlayEvent newObservableEvent)
    {
        if(localPlayer.playerId == newObservableEvent.stringData[0])
        {
            return;
        }
        NetworkedPlayer playerCreatedObserver = GetPlayer(newObservableEvent.stringData[0]);
        Observable observable = playerCreatedObserver.CreateObservableObject(
            prefabName: newObservableEvent.stringData[1],
            startPosition: Util.ConvertFloatArrayToVector3(newObservableEvent.floatData, 0),
            startRotation: Quaternion.Euler(Util.ConvertFloatArrayToVector3(newObservableEvent.floatData, 3)),
            syncOption: (SyncOptions)Enum.ToObject(typeof(SyncOptions), newObservableEvent.integerData[0]),
            interpolatePosition: newObservableEvent.booleanData[0],
            interpolateRotation: newObservableEvent.booleanData[1],
            interpolationFactor: newObservableEvent.floatData[6]);
        ServerCallbacks.newObservableCreated(observable);

    }
    void CreateObservableWithIdObject(GamePlayEvent newObservableEvent)
    {
        if(localPlayer.playerId == newObservableEvent.stringData[0])
        {
            return;
        }
        NetworkedPlayer playerCreatedObserver = GetPlayer(newObservableEvent.stringData[0]);
        Debug.Log($"CREATING OBSERVABLE OBJECTIH WITH ID {newObservableEvent.stringData[2]}");
        Observable observable = playerCreatedObserver.CreateObservableObject(
            prefabName: newObservableEvent.stringData[1],
            uniqueId: newObservableEvent.stringData[2],
            startPosition: Util.ConvertFloatArrayToVector3(newObservableEvent.floatData, 0),
            startRotation: Quaternion.Euler(Util.ConvertFloatArrayToVector3(newObservableEvent.floatData, 3)),
            syncOption: (SyncOptions)Enum.ToObject(typeof(SyncOptions), newObservableEvent.integerData[0]),
            interpolatePosition: newObservableEvent.booleanData[0],
            interpolateRotation: newObservableEvent.booleanData[1],
            interpolationFactor: newObservableEvent.floatData[6]);
        observable.uniqueId = newObservableEvent.stringData[2];
        ServerCallbacks.newObservableWithIdCreated(observable);

    }
    void SyncObject(GamePlayEvent receivedEvent)
    {
        if (receivedEvent.senderId == localPlayer.playerId)
        {
            return;
        }
        NetworkedPlayer sourcePlayer = GetPlayer(receivedEvent.senderId);
        if (sourcePlayer.isLocalPlayer)
        {
            return;
        }
        Observable observableObj;
        int observableIndex = receivedEvent.integerData[1];
        observableObj = sourcePlayer.observer.observables.Find(observer => observer.observableIndex == observableIndex);
        if (observableObj == null)
        {
            Debug.LogError("No observer found with this id " + receivedEvent.integerData[1]);
            return;
        }
        switch (receivedEvent.integerData[0])
        {
            case (int)SyncOptions.SyncPosition:
                observableObj.observeredTransform.transform.position = Util.ConvertFloatArrayToVector3(receivedEvent.floatData, 0);
                break;
            case (int)SyncOptions.SyncRotation:
                observableObj.observeredTransform.transform.rotation = Quaternion.Euler(Util.ConvertFloatArrayToVector3(receivedEvent.floatData, 0));
                break;
            case (int)SyncOptions.SyncPositionAndRotation:
                observableObj.observeredTransform.transform.position = Util.ConvertFloatArrayToVector3(receivedEvent.floatData, 0);
                observableObj.observeredTransform.transform.rotation = Quaternion.Euler(Util.ConvertFloatArrayToVector3(receivedEvent.floatData, 3));
                break;
        }
    }

    void ReflectEvent(GamePlayEvent receivedEvent)
    {
        ServerCallbacks.eventReceived(receivedEvent);
    }
    private void Shuffle<T>(IList<T> list, int seed)
    {
        var rng = new System.Random(seed);
        int n = list.Count;

        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    void CreatePlayers(Player[] gamePlayers)
    {
        if (currentRoomPlayers.Count < 1 && gamePlayers.Length > 1)
        {
            Debug.Log($"ROOM ID IS {gameSession.roomId} AND ITS HASHCODE IS {gameSession.roomId.GetHashCode()}");

            this.Shuffle(SpawnPrefabs,gameSession.roomId.GetHashCode());
            try
            {
                foreach (Player player in gamePlayers)
                {
                    GameObject playerObj = SpawnPrefabs[player.playerIndex].gameObject;
                    playerObj.GetComponent<NetworkedPlayer>().SetUpPlayer(player, gameSession.roomId, player.playerId == gameSession.playerId);

                    GameObject playerCreated;
                    if (WorldOriginTransform != null)
                    {
                        playerCreated = Instantiate(playerObj, WorldOriginTransform);
                        Util.SetLocalPostion(playerCreated.transform, SpawnInfo[player.playerIndex].position);
                        Util.SetLocalRotation(playerCreated.transform, SpawnInfo[player.playerIndex].rotation);
                    }
                    else
                    {
                        playerCreated = Instantiate(playerObj, SpawnInfo[player.playerIndex].position, Quaternion.Euler(SpawnInfo[player.playerIndex].rotation));
                    }
                    NetworkedPlayer networkedPlayer = playerCreated.GetComponent<NetworkedPlayer>();
                    if (player.playerName == "")
                    {
                        playerCreated.name = networkedPlayer.playerName = "Player " + (player.playerIndex + 1);
                    }
                    else
                    {
                        playerCreated.name = player.playerName;
                    }
                    currentRoomPlayers.Add(networkedPlayer);
                    ServerCallbacks.eventReceived += networkedPlayer.OnWebSocketEventReceived;
                    ServerCallbacks.udpEventReceived += networkedPlayer.OnUDPEventReceived;
                    if (player.playerId == gameSession.playerId)
                    {
                        localPlayer = MessageSender = networkedPlayer;
                    }
                }
            }
            catch (NullReferenceException)
            {
                throw new Exception("Error in creating players");
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new Exception("Error in creating players");
            }
            catch (Exception)
            {
                throw new Exception("Zombie Error");
            }
        }
    }
}

