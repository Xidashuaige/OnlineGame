using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

[Serializable]
public class ClientInfo
{
    public ClientInfo(string name, uint id, IPEndPoint endPoint)
    {
        this.name = name;
        this.id = id;
        this.endPoint = endPoint;
        roomId = 0;
    }

    public string name;
    public uint id;
    public uint roomId;
    public IPEndPoint endPoint;
}

public class MessageHandler
{
    public MessageHandler(NetworkMessage message, Action<NetworkMessage> action)
    {
        _message = message;
        _action = action;
    }

    public void Execute()
    {
        _action.Invoke(_message);
    }

    private NetworkMessage _message;
    private Action<NetworkMessage> _action;
}

public class Server : MonoBehaviour
{
    // Unity Objects
    [Space, Header("Global parameters")]
    [SerializeField] private PanelManager _panelManager;
    [SerializeField] private RoomManager _roomManager;
    [SerializeField] private InputController _nameInput;

    // Server parameters
    private Dictionary<uint, ClientInfo> _clients = new();
    private uint _idGen = 0;
    private Dictionary<NetworkMessageType, Action<NetworkMessage>> _actionHandlers = new();
    [SerializeField] private Client _myClient;
    private ConcurrentQueue<MessageHandler> _tasks = new();

    // Socket parameters
    private bool _connecting = false;
    private Socket _socket;
    private const int _serverPort = 8888;
    private readonly object _lock = new();
    private string _ipAdress = "0.0.0.0";

    // Events
    public Action<string> onIpUpdate;
    public Action onServerStart;

    // Handle requests in unity
    private bool _handleStartServer = false;

    // -----------------------------------------------
    // -----------------------------------------------
    // -----------------UNITY EVENTS------------------
    // -----------------------------------------------
    // -----------------------------------------------
    void Start()
    {
        // Init Handle events actions
        _actionHandlers[NetworkMessageType.Heartbeat] = HandleHeartBeatMessage;
        _actionHandlers[NetworkMessageType.StartGame] = HandleStartGameMessage;
        _actionHandlers[NetworkMessageType.ReadyInTheRoom] = HandleReadyInTheRoomMessage;
        _actionHandlers[NetworkMessageType.KickOutRoom] = HandleKickOutRoomMessage;
        _actionHandlers[NetworkMessageType.CreateRoom] = HandleCreateRoomMessage;
        _actionHandlers[NetworkMessageType.JoinRoom] = HandleJoinRoomMessage;
        _actionHandlers[NetworkMessageType.JoinServer] = HandleJoinServerMessage;
        _actionHandlers[NetworkMessageType.LeaveRoom] = HandleLeaveRoomMessage;
        _actionHandlers[NetworkMessageType.LeaveServer] = HandleLeaveServerMessage;
    }

    private void OnApplicationQuit()
    {
        if (_connecting)
        {
            SendMessageToClients(new LeaveServer(_myClient.ID));

            lock (_lock)
                _connecting = false;

            _clients?.Clear();
            _clients = null;
        }

        onIpUpdate = null;
        onServerStart = null;

        _actionHandlers?.Clear();
        _actionHandlers = null;

        _socket?.Dispose();
        _socket = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (_handleStartServer)
        {
            _ipAdress = GetIPAdress();

            onIpUpdate?.Invoke(_ipAdress);

            onServerStart?.Invoke();

            // Change my client to server host
            _myClient.host = true;

            _handleStartServer = false;
        }

        if (_tasks.Count > 0)
        {
            if (_tasks.TryDequeue(out MessageHandler task))
                task.Execute();
        }
    }

    // -----------------------------------------------
    // -----------------------------------------------
    // ---------------SERVER FUNCTIONS----------------
    // -----------------------------------------------
    // -----------------------------------------------
    public void CreateServer()
    {
        if (_connecting)
            return;

        if (_nameInput.Value == "")
            return;

        Thread thread = new(StartServer);

        thread.Start();
    }

    private void StartServer()
    {
        if (_socket == null)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                _socket.Bind(new IPEndPoint(IPAddress.Any, _serverPort));
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);

                _socket.Dispose();
                _socket = null;
                return;
            }
        }

        Debug.Log("Server Start!");

        // When start server successful
        _handleStartServer = true;

        _connecting = true;

        // Start to listen messages
        ListenMessages();
    }

    private void ListenMessages()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        EndPoint _lastEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (_connecting)
        {
            try
            {
                bytesRead = _socket.ReceiveFrom(buffer, ref _lastEndPoint);

                if (bytesRead == 0)
                    continue;

                NetworkMessage message = NetworkPackage.GetDataFromBytes(buffer, bytesRead);

                if (message.type == NetworkMessageType.JoinServer)
                    message.endPoint = _lastEndPoint;

                _tasks.Enqueue(new(message, HandleMessage));

                //ThreadPool.QueueUserWorkItem(new WaitCallback(HandleMessage), message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);

                _connecting = false;

                break;
            }
        }

        buffer = null;
    }

    public void SendMessageToClients(NetworkMessage message)
    {
        Debug.Log("Send message : " + message.type + " to all clients");

        NetworkPackage package = new(message.type, message.GetBytes());

        byte[] data = package.GetBytes();

        foreach (var client in _clients)
        {
            // Send data to server, this function may not block the code
            _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.Value.endPoint, new AsyncCallback(SendCallback), message.type);
        }
    }

    public void SendMessageToClient(ClientInfo client, NetworkMessage message)
    {
        Debug.Log("Send messages : " + message.type + " to client : " + client.name);

        NetworkPackage package = new(message.type, message.GetBytes());

        byte[] data = package.GetBytes();

        Debug.Log("Package Send with lenght: " + data.Length);

        // Send data to server, this function may not block the code
        _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(SendCallback), message.type);
    }

    // After message sent
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            int bytesSent = _socket.EndSend(ar);

            Debug.Log("Server: " + (NetworkMessageType)ar.AsyncState + " send with successful!");
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    // -----------------------------------------------
    // -----------------------------------------------
    // ---------------------UTIL----------------------
    // -----------------------------------------------
    // -----------------------------------------------
    private uint GetNextID()
    {
        return _idGen++;
    }

    public string GetIPAdress()
    {
        IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());

        foreach (IPAddress ip in ipEntry.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }

        return "0.0.0.0";
    }

    public void CopyIPAdress()
    {
        GUIUtility.systemCopyBuffer = _ipAdress;
        Debug.Log("IP Adress Copied");
    }

    // -----------------------------------------------
    // -----------------------------------------------
    // ------------HANDLE PLAYER MESSAGES-------------
    // -----------------------------------------------
    // -----------------------------------------------
    private void HandleMessage(object messageObj)
    {
        var message = messageObj as NetworkMessage;

        message.succesful = true;

        _actionHandlers[message.type].Invoke(message);
    }

    private void HandleHeartBeatMessage(NetworkMessage data)
    {
        var message = data as HearthBeat;
    }

    private void HandleJoinServerMessage(NetworkMessage data)
    {
        var message = data as JoinServer;

        ClientInfo client = new(message.name, GetNextID(), message.endPoint as IPEndPoint);

        _clients.Add(client.id, client);

        message.messageOwnerId = client.id;

        SendMessageToClient(client, message);
    }

    private void HandleLeaveServerMessage(NetworkMessage data)
    {
        var message = data as LeaveServer;

        if (message.messageOwnerId == _myClient.ID)
        {
            SendMessageToClients(message);

            _clients.Clear();

            _socket?.Dispose();

            Debug.Log("Server Closed!");

            lock (_lock)
                _connecting = false;
        }
        else
        {
            ClientInfo client = _clients[message.messageOwnerId];

            _clients.Remove(client.id);

            SendMessageToClient(client, message);
        }
    }

    private void HandleCreateRoomMessage(NetworkMessage data)
    {
        var message = data as CreateRoom;

        Debug.Log("Server: some player request for create a room");

        var client = _clients[message.messageOwnerId];

        message.roomId = _roomManager.CreateRoomFromServer(client, message.maxUser);

        message.roomMaseter = client;

        _clients[message.messageOwnerId].roomId = message.roomId;

        if (message.roomId != 0)
        {
            message.succesful = true;

            Debug.Log("Server: Room create successful!");

            SendMessageToClients(message);
        }

        else
        {
            message.succesful = false;

            Debug.Log("Server: Room create faild!");

            SendMessageToClient(client, message);
        }
    }

    private void HandleJoinRoomMessage(NetworkMessage data)
    {
        var message = data as JoinRoom;
    }

    private void HandleLeaveRoomMessage(NetworkMessage data)
    {
        var message = data as LeaveRoom;

        // TODO
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
}
