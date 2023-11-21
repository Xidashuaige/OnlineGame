using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class Client : MonoBehaviour
{
    #region Paramaters
    // Unity paramaters
    [SerializeField] private InputController _ipInput;
    [SerializeField] private InputController _nameInput;

    // General
    [SerializeField] private Server _server;
    [SerializeField] private RoomManager _roomManager;

    // Clients paramaters
    private const int SERVER_PORT = 8888;

    private IPEndPoint _serverEndPoint;

    private Socket _socket;

    private bool _connecting;

    public bool host = false;

    public uint ID { get => _id; }

    private uint _id = 0;

    private uint _roomId = 0;

    // Callbacks
    private Dictionary<NetworkMessageType, Action<NetworkMessage>> _actionHandlers = new();
    private Dictionary<NetworkMessageType, Action<bool>> _actionSuccessful = new();

    // locker
    private readonly object _lock = new();

    // Handle requests in unity
    private ConcurrentQueue<MessageHandler> _tasks = new();

    // Events
    public Action onJoinServer = null;
    public Action onLeaveServer = null;
    public Action<JoinRoom> onJoinRoom = null;

    #endregion

    #region Unity events
    private void Start()
    {
        #region Init Handle events actions

        _actionHandlers[NetworkMessageType.Heartbeat] = HandleHeartBeatMessage;
        _actionHandlers[NetworkMessageType.StartGame] = HandleStartGameMessage;
        _actionHandlers[NetworkMessageType.ReadyInTheRoom] = HandleReadyInTheRoomMessage;
        _actionHandlers[NetworkMessageType.KickOutRoom] = HandleKickOutRoomMessage;
        _actionHandlers[NetworkMessageType.CreateRoom] = HandleCreateRoomMessage;
        _actionHandlers[NetworkMessageType.JoinRoom] = HandleJoinRoomMessage;
        _actionHandlers[NetworkMessageType.JoinServer] = HandleJoinServerMessage;
        _actionHandlers[NetworkMessageType.LeaveRoom] = HandleLeaveRoomMessage;
        _actionHandlers[NetworkMessageType.LeaveServer] = HandleLeaveServerMessage;

        #endregion

        #region Init successful events actions

        _actionSuccessful[NetworkMessageType.Heartbeat] = WhenHeartBeatHasSent;
        _actionSuccessful[NetworkMessageType.StartGame] = WhenStartGameHasSent;
        _actionSuccessful[NetworkMessageType.ReadyInTheRoom] = WhenReadyInTheRoomHasSent;
        _actionSuccessful[NetworkMessageType.KickOutRoom] = WhenKickOutRoomHasSent;
        _actionSuccessful[NetworkMessageType.CreateRoom] = WhenCreateRoomHasSent;
        _actionSuccessful[NetworkMessageType.JoinRoom] = WhenJoinRoomHasSent;
        _actionSuccessful[NetworkMessageType.JoinServer] = WhenJoinServerHasSent;
        _actionSuccessful[NetworkMessageType.LeaveRoom] = WhenLeaveRoomHasSent;
        _actionSuccessful[NetworkMessageType.LeaveServer] = WhenLeaveServerHasSent;

        #endregion

        _server.onServerStart += RequestJoinToServer;
    }

    private void Update()
    {
        // Do handle task in main thread
        if (_tasks.Count > 0)
        {
            if (_tasks.TryDequeue(out MessageHandler task))
                task.Execute();
        }
    }

    private void OnApplicationQuit()
    {
        onJoinServer = null;
        onLeaveServer = null;

        _actionHandlers?.Clear();
        _actionHandlers = null;

        _actionSuccessful?.Clear();
        _actionSuccessful = null;

        _socket?.Dispose();
        _socket = null;
    }

    #endregion

    #region Requests to Server

    public void RequestJoinToServer()
    {
        if (_connecting)
            return;

        if (_nameInput.Value == "")
            return;

        if (_ipInput.Value == "")
            return;

        try
        {
            // Create socket and and serverEndPoint
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            _serverEndPoint = new(IPAddress.Parse(_ipInput.Value), SERVER_PORT);
        }
        catch (Exception e)
        {
            _socket?.Dispose();
            _socket = null;

            Debug.Log(e);
            return;
        }

        _connecting = true;

        // Try to connect to server
        var messagePackage = NetworkPackage.CreateJoinServerRequest(_nameInput.Value);

        SendMessageToServer(messagePackage);

        // Start to listen messages from server
        Thread thread = new(ListenMessages);

        thread.Start();
    }

    public void RequestLeaveTheServer()
    {
        if (!_connecting)
            return;

        var messagePackage = NetworkPackage.CreateLeaveServerRequest(_id);

        SendMessageToServer(messagePackage);
    }

    public void RequestCreateRoom()
    {
        if (!_connecting)
            return;

        var messagePackage = NetworkPackage.CreateCreateRoomRequest(_id);

        SendMessageToServer(messagePackage);
    }

    public void RequestJoinRoom(uint roomId)
    {
        if (!_connecting)
            return;

        var messagePackage = NetworkPackage.CreateJoinRoomRequest(_id, roomId, _nameInput.Value);

        SendMessageToServer(messagePackage);
    }

    public void RequestLeaveRoom()
    {
        if (!_connecting)
            return;

        var messagePackage = NetworkPackage.CreateLeaveRoomRequest(_id, _roomId);

        SendMessageToServer(messagePackage);
    }

    #endregion

    #region Socket related functions

    private void ListenMessages()
    {
        byte[] buffer = new byte[1024];
        int bytesRead = 0;

        Debug.Log("Client " + _nameInput.Value + " start receive message");

        while (_connecting)
        {
            try
            {
                bytesRead = _socket.Receive(buffer);

                Debug.Log("Package recived with lenght: " + bytesRead);

                NetworkMessage message = NetworkPackage.GetDataFromBytes(buffer, bytesRead);

                if (bytesRead <= 0)
                {
                    lock (_lock)
                        _connecting = false;

                    Debug.LogWarning("Message Receive with error, disconnect from server");

                    return;
                }

                _tasks.Enqueue(new(message, HandleMessage));

                //ThreadPool.QueueUserWorkItem(new WaitCallback(HandleMessage), message);
            }
            catch (SocketException ex)
            {
                // I DON'T KNOW WHY HAVE I THIS ERROR!!!!
                if (ex.ErrorCode == (int)SocketError.InvalidArgument)
                {
                    Debug.LogWarning(ex.Message);
                    continue;
                }

                LeaveServer leaveServer = new(_id, true);

                _tasks.Enqueue(new(leaveServer, HandleMessage));

                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);

                LeaveServer leaveServer = new(_id, true);

                _tasks.Enqueue(new(leaveServer, HandleMessage));

                break;
            }
        }
    }

    public void SendMessageToServer(NetworkPackage messagePackage)
    {
        byte[] data = messagePackage.GetBytes();

        // Send data to server
        _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, _serverEndPoint, new AsyncCallback(SendCallback), messagePackage.type);
    }

    // After message sent
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            int bytesSent = _socket.EndSend(ar);
            Debug.Log("Client " + _nameInput.Value + " " + (NetworkMessageType)ar.AsyncState + " send with successful!");

            _actionSuccessful[(NetworkMessageType)ar.AsyncState]?.Invoke(true);

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());

            _actionSuccessful[(NetworkMessageType)ar.AsyncState]?.Invoke(false);
        }
    }

    #endregion

    // -----------------------------------------------
    // -------WHEN RECEIVE MESSAGE FROM SERVER--------
    // -----------------------------------------------

    #region When recive message from server

    private void HandleMessage(object messageObj)
    {
        var message = messageObj as NetworkMessage;

        _actionHandlers[message.type].Invoke(message);
    }

    private void HandleHeartBeatMessage(NetworkMessage data)
    {
        var message = data as HearthBeat;

        Debug.Log("HeartBeat from server");
    }

    private void HandleJoinServerMessage(NetworkMessage data)
    {
        var message = data as JoinServer;

        if (message.succesful)
        {
            _id = message.messageOwnerId;

            onJoinServer.Invoke();

            Debug.Log("Join Server Successful");

            return;
        }

        Debug.Log("Join Server Faild");
    }

    private void HandleLeaveServerMessage(NetworkMessage data)
    {
        var message = data as LeaveServer;

        if (message.succesful)
        {
            _socket?.Dispose();

            lock (_lock)
                _connecting = false;

            onLeaveServer.Invoke();

            Debug.Log("Leave Server Successful");
        }
    }

    private void HandleCreateRoomMessage(NetworkMessage data)
    {
        var message = data as CreateRoom;

        if (message.succesful)
        {
            _roomManager.CreateRoom(message.roomMaster, message.roomId, message.maxUser);

            if (_id == message.messageOwnerId)
            {
                _roomId = message.roomId;

                //onJoinRoom.Invoke(new(_id, message.roomId, _nameInput.Value, true));

                Debug.Log("Create the room successful!");

                var messagePackage = NetworkPackage.CreateJoinRoomRequest(_id, _roomId, _nameInput.Value, true);

                SendMessageToServer(messagePackage);
            }
            else
            {
                Debug.Log("Some player create a room");
            }
        }
        else
        {
            Debug.Log("Create room faild: Is max number of rooms yet");
        }
    }

    private void HandleJoinRoomMessage(NetworkMessage data)
    {
        var message = data as JoinRoom;

        if (message.messageOwnerId == ID)
        {
            _roomId = message.roomId;
            Debug.Log("Join the room successful!");
        }
        else
        {
            Debug.Log("Someone enter the room!");
        }

        if (!host)
            _roomManager.JoinRoom(message);

        onJoinRoom.Invoke(message);
    }

    private void HandleLeaveRoomMessage(NetworkMessage data)
    {
        var message = data as LeaveRoom;
    }

    private void HandleReadyInTheRoomMessage(NetworkMessage data)
    {
        var message = data as ReadyInTheRoom;
    }

    private void HandleStartGameMessage(NetworkMessage data)
    {
        var message = data as StartGame;
    }

    private void HandleKickOutRoomMessage(NetworkMessage data)
    {
        var message = data as KickOutRoom;
    }

    #endregion

    // -----------------------------------------------
    // ----------WHEN MESSAGE SENT SUCCESSFUL---------
    // -----------------------------------------------

    #region When message sent successful
    private void WhenHeartBeatHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }

    private void WhenJoinServerHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenLeaveServerHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenCreateRoomHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenJoinRoomHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenLeaveRoomHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenReadyInTheRoomHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenStartGameHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenKickOutRoomHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }

    #endregion

    // -----------------------------------------------
    // ----------------UTIL FUNCTIONS-----------------
    // -----------------------------------------------
}
