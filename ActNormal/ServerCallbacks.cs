using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ServerCallbacks : MonoBehaviour
{
    public static Action connectedToServer;
    public static Action<string> failureToConnect;
    public static Action registerEvent;
    public static Action<Notification> notificationEvent;
    public static Action<GamePlayEvent> eventReceived;
    public static Action<GamePlayEvent> udpEventReceived;
    public static Action<RoomMemberLeft> playerLeft;
    public static Action<List<Room>> roomsList;
    public static Action<Room> roomCreated;
    public static Action<Room> roomJoin;
    public static Action<Room> playerRoomJoined;
    public static Action joinRoomFaliure;
    public static Action newRoomCreatedInLobby;
    public static Action roomRemovedFromLobby;
    public static Action leftRoom;
    public static Action gameStart;
    public static Action gameEnd;
    public static Action<Observable> newObservableCreated;
    public static Action<Observable> newObservableWithIdCreated;

    public void ConnectToServer(string path = "")
    {
        connectedToServer += OnConnectionToServer;
        notificationEvent += OnNotificationEvent;
        newRoomCreatedInLobby += OnNewRoomCreatedInLobby;
        roomCreated += OnRoomCreated;
        roomJoin += OnRoomJoin;
        playerRoomJoined += PlayerJoinedRoom;
        joinRoomFaliure += OnJoinRoomFailed;
        gameStart += OnGameStart;
        playerLeft += OnPlayerLeft;
        leftRoom += OnLeftRoom;
        eventReceived += OnWebSocketEventReceived;
        udpEventReceived += OnUDPEventReceived;
        newObservableCreated += OnNewObservableCreated;
        newObservableWithIdCreated += OnNewObservableWithIdCreated;
        StartCoroutine(ConnectToServerCoroutine(path));
    }

    IEnumerator ConnectToServerCoroutine(string path = "")
    {
        ServerManager serverManager = FindObjectOfType<ServerManager>();
        serverManager.ConnectToServer(path).ConfigureAwait(false);
    }
    public virtual void OnConnectionToServer()
    {
        Debug.Log("Connected to Server");
    }

    public virtual void OnNotificationEvent(Notification notification)
    {
        Debug.Log("Notification Event From Server :"+ notification.notificationText);
    }

    public virtual void OnNewRoomCreatedInLobby ()
    {
        Debug.Log("New Room Created In the Lobby, Call ServerManager.GetRooms() to get the updated rooms list");
    }

    public virtual void OnRoomCreated(Room room)
    {
        Debug.Log("Room Created Event From Server");
    }

    public virtual void OnRoomJoin(Room room)
    {
        Debug.Log("Room Join Event From Server");
    }

    public virtual void PlayerJoinedRoom(Room room)
    {
        Debug.Log("PlayerJoinedRoom Event From Server");
    }

    public virtual void OnJoinRoomFailed()
    {
        Debug.Log("Join Room Failed Event From Server");
    }

    public virtual void OnGameStart()
    {
        Debug.Log("Game Start Event From Server");
    }

    public virtual void OnPlayerLeft(RoomMemberLeft playerLeft)
    {
        Debug.Log("Player Left Event From Server");
    }

    public virtual void OnLeftRoom()
    {
        Debug.Log("Left Room Event From Server");
    }

    public virtual void OnWebSocketEventReceived(GamePlayEvent gamePlayEvent)
    {
        // Debug.Log("WebSocket Event Received From Server : " + gamePlayEvent.eventName);
    }

    public virtual void OnNewObservableCreated(Observable observable)
    {
        Debug.Log("New Observable created, owner name : " + observable.owner.playerName);
    }
    public virtual void OnNewObservableWithIdCreated(Observable observable)
    {
        Debug.Log("New Observable with shared id created, owner name : " + observable.owner.playerName);
    }

    public virtual void OnUDPEventReceived(GamePlayEvent gamePlayEvent)
    {
        //Debug.Log("UDP Msg Received Event From Server : " + gamePlayEvent.eventName);
    }

    private void OnDestroy()
    {
        connectedToServer -= OnConnectionToServer;
        notificationEvent -= OnNotificationEvent;
        newRoomCreatedInLobby -= OnNewRoomCreatedInLobby;
        roomCreated -= OnRoomCreated;
        roomJoin -= OnRoomJoin;
        playerRoomJoined -= PlayerJoinedRoom;
        joinRoomFaliure -= OnJoinRoomFailed;
        gameStart -= OnGameStart;
        playerLeft -= OnPlayerLeft;
        leftRoom -= OnLeftRoom;
        eventReceived -= OnWebSocketEventReceived;
        udpEventReceived -= OnUDPEventReceived;
        newObservableCreated -= OnNewObservableCreated;
        newObservableWithIdCreated -= OnNewObservableWithIdCreated;
    }
}

