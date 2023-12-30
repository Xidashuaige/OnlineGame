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
    public static Client Instance = null;

    // Unity paramaters
    [SerializeField] private InputController _ipInput;
    [SerializeField] private InputController _nameInput;

    // Clients paramaters
    private const int SERVER_PORT = 8888;

    private IPEndPoint _serverEndPoint;

    private Socket _socket;

    private bool _connecting;

    public bool host = false;

    public string Name { get => _nameInput.Value; }

    private uint _id = 0;
    public uint ID { get => _id; }

    // Room paramaters
    private uint _roomId = 0;
    public uint RoomID { get => _roomId; }

    private bool _ImRoomMaster = false;
    public bool RoomMaster { get => _ImRoomMaster; }

    // Callbacks
    private Dictionary<NetworkMessageType, Action<NetworkMessage>> _actionHandlers = new();
    private Dictionary<NetworkMessageType, Action<bool>> _onActionSuccessful = new();

    // locker
    private readonly object _lock = new();

    // Handle requests in unity
    private ConcurrentQueue<MessageHandler> _tasks = new();

    // Events
    public Dictionary<NetworkMessageType, Action<NetworkMessage>> onActionHandlered = new();

    #endregion

    #region Unity events

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject.transform.parent);
        }
        else
        {
            Destroy(gameObject);
        }

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

        _actionHandlers[NetworkMessageType.UpdatePlayerPosition] = HandleUpdatePlayerPosition;
        _actionHandlers[NetworkMessageType.UpdateBirdPosition] = HandleUpdateBirdPosition;
        _actionHandlers[NetworkMessageType.UpdateBombPosition] = HandleUpdateBombPosition;
        _actionHandlers[NetworkMessageType.Explotion] = HandleExplotion;
        _actionHandlers[NetworkMessageType.PlayerDead] = HandlePlayerDead;
        _actionHandlers[NetworkMessageType.UpdateGameWorld] = HandleUpdateGameWorld;
        #endregion

        #region Init successful events actions

        _onActionSuccessful[NetworkMessageType.Heartbeat] = WhenHeartBeatHasSent;
        _onActionSuccessful[NetworkMessageType.StartGame] = WhenStartGameHasSent;
        _onActionSuccessful[NetworkMessageType.ReadyInTheRoom] = WhenReadyInTheRoomHasSent;
        _onActionSuccessful[NetworkMessageType.KickOutRoom] = WhenKickOutRoomHasSent;
        _onActionSuccessful[NetworkMessageType.CreateRoom] = WhenCreateRoomHasSent;
        _onActionSuccessful[NetworkMessageType.JoinRoom] = WhenJoinRoomHasSent;
        _onActionSuccessful[NetworkMessageType.JoinServer] = WhenJoinServerHasSent;
        _onActionSuccessful[NetworkMessageType.LeaveRoom] = WhenLeaveRoomHasSent;
        _onActionSuccessful[NetworkMessageType.LeaveServer] = WhenLeaveServerHasSent;

        #endregion

        // Init callback events actions
        for (int i = 0; i < (int)NetworkMessageType.MaxCount; i++)
        {
            onActionHandlered.Add((NetworkMessageType)i, null);
        }
    }

    private void Start()
    {
        Server.Instance.onServerStart += RequestJoinToServer;
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
        onActionHandlered?.Clear();
        onActionHandlered = null;

        _actionHandlers?.Clear();
        _actionHandlers = null;

        _onActionSuccessful?.Clear();
        _onActionSuccessful = null;

        _socket?.Dispose();
        _socket = null;
    }

    #endregion


    // -----------------------------------------------
    // --------------REQEUST TP SERVER----------------
    // -----------------------------------------------
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

            //Debug.Log("SendBuffer: " + _socket.SendBufferSize);
            //Debug.Log("ReceiveBuffer: " + _socket.ReceiveBufferSize);

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
        var message = new JoinServer(_nameInput.Value);

        SendMessageToServer(message);

        // Start to listen messages from server
        Thread thread = new(ListenMessages);

        thread.Start();
    }

    public void RequestLeaveTheServer()
    {
        if (!_connecting)
            return;

        var message = new LeaveServer(_id);

        SendMessageToServer(message);
    }

    public void RequestCreateRoom()
    {
        if (!_connecting)
            return;

        var message = new CreateRoom(_id);

        SendMessageToServer(message);
    }

    public void RequestJoinRoom(uint roomId)
    {
        if (!_connecting)
            return;

        var message = new JoinRoom(_id, roomId, _nameInput.Value);

        SendMessageToServer(message);
    }

    public void RequestLeaveRoom()
    {
        if (!_connecting)
            return;

        var message = new LeaveRoom(_id, _roomId);

        SendMessageToServer(message);
    }

    public void RequestStartGame()
    {
        if (!_connecting || !RoomMaster)
            return;

        var message = new StartGame(_id, _roomId);

        SendMessageToServer(message);
    }

    public void RequestMovePlayer(uint netID, Vector2 newPosition, bool flipX, float timeUsed)
    {
        if (!_connecting)
            return;

        var message = new UpdatePlayerMovement(_id, netID, newPosition, flipX, timeUsed);

        SendMessageToServer(message);
    }

    public void RequestMoveBird(uint netID, Vector2 newPosition, bool flipX, float timeUsed)
    {
        if (!_connecting)
            return;

        var message = new UpdateBirdMovement(_id, netID, newPosition, flipX, timeUsed);

        SendMessageToServer(message);
    }

    public void RequestMoveBomb(uint netID, Vector2 newPosition, float timeUsed)
    {
        if (!_connecting)
            return;

        var message = new UpdateBombMovement(_id, netID, newPosition, timeUsed);

        SendMessageToServer(message);
    }

    public void RequestExplotion(uint netID, Vector2 position)
    {
        if (!_connecting)
            return;

        var message = new ExplotionMessage(_id, netID, position);

        SendMessageToServer(message);
    }

    public void RequestPlayerDead(uint netID)
    {
        if (!_connecting)
            return;

        var message = new PlayerDead(_id, netID, RoomID);

        SendMessageToServer(message);
    }

    public void RequestUpdateGameWorld()
    {
        // update all world information
    }

    #endregion

    // -----------------------------------------------
    // -----------SOCKET RELATED FUNCIONS-------------
    // -----------------------------------------------

    #region Socket related functions

    private void ListenMessages()
    {
        byte[] buffer = new byte[2048];
        int bytesRead = 0;

        Debug.Log("Client (" + _nameInput.Value + "): start receive message");

        while (_connecting)
        {
            try
            {
                bytesRead = _socket.Receive(buffer);

                Debug.Log("Client (" + _nameInput.Value + "): package recived with lenght: " + bytesRead);

                NetworkMessage message = NetworkPackage.GetDataFromBytes(buffer, bytesRead);

                if (bytesRead <= 0)
                {
                    lock (_lock)
                        _connecting = false;

                    Debug.LogWarning("Client (" + _nameInput.Value + "): message Receive with error, disconnect from server");

                    return;
                }

                _tasks.Enqueue(new(message, HandleMessage));

                //ThreadPool.QueueUserWorkItem(new WaitCallback(HandleMessage), message);
            }
            catch (SocketException ex)
            {
                Debug.LogWarning(ex.Message);

                // I DON'T KNOW WHY HAVE I THIS ERROR!!!!
                if (ex.ErrorCode == (int)SocketError.InvalidArgument)
                    continue;

                _connecting = false;

                LeaveServer leaveServer = new(_id, true);

                _tasks.Enqueue(new(leaveServer, HandleMessage));

                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);

                _connecting = false;

                LeaveServer leaveServer = new(_id, true);

                _tasks.Enqueue(new(leaveServer, HandleMessage));

                break;
            }
        }
    }
    public void SendMessageToServer(NetworkMessage message)
    {
        NetworkPackage messagePackage = new(message.type, message.GetBytes());

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
            //Debug.Log("Client " + _nameInput.Value + " " + (NetworkMessageType)ar.AsyncState + " send with successful!");

            //_actionSuccessful[(NetworkMessageType)ar.AsyncState]?.Invoke(true);

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());

            //_actionSuccessful[(NetworkMessageType)ar.AsyncState]?.Invoke(false);
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

            onActionHandlered[NetworkMessageType.JoinServer]?.Invoke(message);

            Debug.Log("Client(" + Name + "): join server successful");

            return;
        }

        Debug.Log("Client(" + Name + "): join server faild");
    }

    private void HandleLeaveServerMessage(NetworkMessage data)
    {
        var message = data as LeaveServer;

        if (message.succesful)
        {
            _socket?.Dispose();

            lock (_lock)
                _connecting = false;

            onActionHandlered[NetworkMessageType.LeaveServer]?.Invoke(message);

            Debug.Log("Client(" + Name + "): leave server successful");
        }
    }

    private void HandleCreateRoomMessage(NetworkMessage data)
    {
        var message = data as CreateRoom;

        if (!message.succesful)
        {
            Debug.Log("Client (" + Name + "): create room faild: Is max number of rooms yet");
            return;
        }

        onActionHandlered[NetworkMessageType.CreateRoom]?.Invoke(message);

        if (message.messageOwnerId == _id)
        {
            _roomId = message.roomId;

            Debug.Log("Client (" + Name + "): create the room successful! with room id: " + _roomId);

            Debug.Log("Client (" + Name + "): try to enter room " + _roomId);

            SendMessageToServer(new JoinRoom(_id, _roomId, _nameInput.Value, true));
        }
        else
        {
            Debug.Log("Client (" + Name + "): some player create a room");
        }
    }

    private void HandleJoinRoomMessage(NetworkMessage data)
    {
        var message = data as JoinRoom;

        if (message.succesful == false)
            return;

        if (message.messageOwnerId == ID)
        {
            _ImRoomMaster = message.client.isRoomMaster;
            _roomId = message.roomId;
        }

        onActionHandlered[NetworkMessageType.JoinRoom]?.Invoke(message);

        if (_roomId == message.roomId)
        {
            Debug.Log("Client(" + Name + "): join the room successful!");
        }
        else
        {
            Debug.Log("Client(" + Name + "): (" + message.client.name + ") entered the room!");
        }
    }

    private void HandleLeaveRoomMessage(NetworkMessage data)
    {
        var message = data as LeaveRoom;

        if (!message.succesful)
        {
            Debug.Log("Client(" + Name + "): someone leave room faild");
            return;
        }

        onActionHandlered[NetworkMessageType.LeaveRoom]?.Invoke(message);

        if (message.isRoomMaster)
        {
            Debug.Log("Client(" + Name + "): room master leave the room(" + message.roomId.ToString("D4") + ")!");

            _roomId = 0;
            _ImRoomMaster = false;
        }
        else
        {
            Debug.Log("Client(" + Name + "): someone leave the room(" + message.roomId.ToString("D4") + ")!");
        }
    }

    private void HandleReadyInTheRoomMessage(NetworkMessage data)
    {
        var message = data as ReadyInTheRoom;
    }

    private void HandleStartGameMessage(NetworkMessage data)
    {
        var message = data as StartGame;

        if (!message.succesful)
        {
            Debug.Log("Client(" + Name + "): Start Game fail");
            return;
        }

        onActionHandlered[NetworkMessageType.StartGame]?.Invoke(message);

        Debug.Log("Client(" + Name + "): Start Game successful");
        return;
    }

    private void HandleKickOutRoomMessage(NetworkMessage data)
    {
        var message = data as KickOutRoom;
    }

    private void HandleUpdatePlayerPosition(NetworkMessage data)
    {
        if (!data.succesful || data.messageOwnerId == ID)
            return;

        onActionHandlered[data.type]?.Invoke(data);
    }

    private void HandleUpdateBirdPosition(NetworkMessage data)
    {
        if (!data.succesful || data.messageOwnerId == ID)
            return;

        onActionHandlered[data.type]?.Invoke(data);
    }

    private void HandleUpdateBombPosition(NetworkMessage data)
    {
        if (!data.succesful)
            return;

        onActionHandlered[data.type]?.Invoke(data);
    }

    private void HandleExplotion(NetworkMessage data)
    {
        if (!data.succesful || data.messageOwnerId == ID)
            return;

        onActionHandlered[data.type]?.Invoke(data);
    }

    private void HandlePlayerDead(NetworkMessage data)
    {
        var message = data as PlayerDead;

        if (message.succesful && RoomID == message.roomId)
            onActionHandlered[data.type]?.Invoke(data);
    }

    private void HandleUpdateGameWorld(NetworkMessage data)
    {
        if (!data.succesful || data.messageOwnerId == ID)
            return;

        onActionHandlered[data.type]?.Invoke(data);
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
