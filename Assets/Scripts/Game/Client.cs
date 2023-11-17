using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class Client : MonoBehaviour
{
    #region Paramaters
    // Unity paramaters
    [SerializeField] private PanelManager _panelManager;
    [SerializeField] private InputController _ipInput;
    [SerializeField] private InputController _nameInput;
    [SerializeField] private Server _server;

    // Clients paramaters
    private IPEndPoint _serverEndPoint;

    private Socket _socket;

    private bool _connecting;

    public bool host = false;

    private const int SERVER_PORT = 8888;

    public uint ID { get => _id; }

    private uint _id;

    private Dictionary<NetworkMessageType, Action<NetworkMessage>> _actionHandlers = new();

    private Dictionary<NetworkMessageType, Action<bool>> _actionSuccessful = new();

    private readonly object _lock = new();

    // Handle requests in unity
    private bool _handleJoinServer = false;
    private bool _handleLeaveServer = false;

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
        if (_handleJoinServer)
        {
            _panelManager.ChangeScene(Panels.RoomListPanel);

            _handleJoinServer = false;
        }

        if (_handleLeaveServer)
        {
            _panelManager.ChangeScene(Panels.StartPanel);

            _handleLeaveServer = false;
        }
    }

    private void OnApplicationQuit()
    {
        _connecting = false;

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

        if (_socket == null)
        {
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

    #endregion

    #region Socket related functions

    private void ListenMessages()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        Debug.Log("Client " + _nameInput.Value + " start receive message");

        while (_connecting)
        {
            try
            {
                bytesRead = _socket.Receive(buffer);

                NetworkMessage message = NetworkPackage.GetDataFromBytes(buffer, bytesRead);

                if (bytesRead <= 0)
                {
                    lock (_lock)
                        _connecting = false;

                    Debug.LogWarning("Message Receive with error, disconnect from server");

                    return;
                }

                HandleMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);

                lock (_lock)
                    _handleLeaveServer = true;

                lock (_lock)
                    _connecting = false;
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

    private void HandleMessage(NetworkMessage message)
    {
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

            _handleJoinServer = true;

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
            _handleLeaveServer = true;

            Debug.Log("Leave Server Successful");
        }
    }

    private void HandleCreateRoomMessage(NetworkMessage data)
    {
        var message = data as CreateRoom;
    }

    private void HandleJoinRoomMessage(NetworkMessage data)
    {
        var message = data as JoinRoom;
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
