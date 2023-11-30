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
    public ClientInfo(string name, uint id, IPEndPoint endPoint = null, bool isRoomMaster = false)
    {
        this.name = name;
        this.id = id;
        this.endPoint = endPoint;
        this.isRoomMaster = isRoomMaster;
        roomId = 0;
    }

    public string name;
    public uint id;
    public IPEndPoint endPoint;

    // room info
    public uint roomId; // 0 if is not in any room
    public bool isRoomMaster = false;
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
    public static Server Instance = null;

    // Unity Objects
    [Header("Global parameters")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private InputController _nameInput;

    // Server parameters
    private Dictionary<uint, ClientInfo> _clients = new();
    private uint _idGen = 0;
    private uint _netIdGen = 0;
    private Dictionary<NetworkMessageType, Action<NetworkMessage>> _actionHandlers = new();
    private ConcurrentQueue<MessageHandler> _tasks = new();

    // Socket parameters
    private bool _connecting = false;
    private Socket _socket;
    private const int _serverPort = 8888;
    private readonly object _lock = new();
    private string _ipAdress = "0.0.0.0";
    private int _messageHandleFlag = 0;

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
    }

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

        _actionHandlers[NetworkMessageType.UpdatePlayerPosition] = HandleUpdatePlayerMovement;

        _gameManager = FindAnyObjectByType<GameManager>();
    }

    private void OnApplicationQuit()
    {
        if (_connecting)
        {
            SendMessageToClients(new LeaveServer(Client.Instante.ID));

            while (_messageHandleFlag > 0)
            {
                Debug.Log("Server: waiting for exit all clients");
            }

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
            Client.Instante.host = true;

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

        Debug.Log("Server Start!");

        // When start server successful
        _handleStartServer = true;

        _connecting = true;

        // Start to listen messages
        ListenMessages();
    }

    private void ListenMessages()
    {
        byte[] buffer = new byte[2048];
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
        Debug.Log("Server: send message [" + message.type + "] to all clients");

        NetworkPackage package = new(message.type, message.GetBytes());

        byte[] data = package.GetBytes();

        foreach (var client in _clients)
        {
            lock (_lock)
                _messageHandleFlag++;

            // Send data to server, this function may not block the code
            _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.Value.endPoint, new AsyncCallback(SendCallback), message.type);
        }
    }

    public void SendMessageToClients(ClientInfo[] clients, NetworkMessage message)
    {
        Debug.Log("Server: send message [" + message.type + "] to select clients");

        NetworkPackage package = new(message.type, message.GetBytes());

        byte[] data = package.GetBytes();

        foreach (var client in clients)
        {
            lock (_lock)
                _messageHandleFlag++;

            // Send data to server, this function may not block the code
            _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(SendCallback), message.type);
        }
    }

    public void SendMessageToClient(ClientInfo client, NetworkMessage message)
    {
        Debug.Log("Server: send messages [" + message.type + "] to client (" + client.name + ")");

        NetworkPackage package = new(message.type, message.GetBytes());

        byte[] data = package.GetBytes();

        //Debug.Log("Server : package send with lenght: " + data.Length);

        lock (_lock)
            _messageHandleFlag++;

        // Send data to server, this function may not block the code
        _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(SendCallback), message.type);
    }

    // After message sent
    private void SendCallback(IAsyncResult ar)
    {
        lock (_lock)
            _messageHandleFlag--;

        try
        {
            int bytesSent = _socket.EndSend(ar);

            Debug.Log("Server: [" + (NetworkMessageType)ar.AsyncState + "] send with successful!");
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
        return ++_idGen;
    }

    private uint GetNextNetID()
    {
        return ++_netIdGen;
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

        if (message.type == NetworkMessageType.Null)
        {
            message.succesful = false;

            Debug.Log("Server: undefined message");

            return;
        }

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

        message.AddRooms(RoomManager.Instance.GetActiveRooms());

        message.succesful = true;

        SendMessageToClient(client, message);
    }

    private void HandleLeaveServerMessage(NetworkMessage data)
    {
        var message = data as LeaveServer;

        message.succesful = true;

        if (message.messageOwnerId == Client.Instante.ID)
        {
            SendMessageToClients(message);

            while (_messageHandleFlag != 0)
            {
                Debug.Log("Server :waiting for Server close action!");
            }

            _clients.Clear();

            _socket?.Dispose();

            Debug.Log("Server: Server Closed!");

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

        var newRoom = RoomManager.Instance.CreateRoomFromServer(message);

        if (newRoom == null)
        {
            message.succesful = false;

            SendMessageToClient(_clients[message.messageOwnerId], message);

            return;
        }

        message.succesful = true;

        message.roomId = newRoom.id;

        SendMessageToClients(message);
    }

    private void HandleJoinRoomMessage(NetworkMessage data)
    {
        var message = data as JoinRoom;

        var sender = _clients[message.messageOwnerId];

        // if client already have room or room is not avaliable for moment
        if (sender.roomId != 0 || !RoomManager.Instance.CheckIfRoomAvaliable(message.roomId))
        {
            message.succesful = false;

            SendMessageToClient(sender, message);
        }
        else
        {
            message.client = sender;

            message.succesful = RoomManager.Instance.JoinRoomFromServer(message);

            SendMessageToClient(sender, message);

            message.clientsInTheRoom = null;

            var otherClients = _clients.Values.Where(client => client.id != message.messageOwnerId).ToArray();

            SendMessageToClients(otherClients, message);
        }
    }

    private void HandleLeaveRoomMessage(NetworkMessage data)
    {
        var message = data as LeaveRoom;

        var sender = _clients[message.messageOwnerId];

        message.isRoomMaster = sender.isRoomMaster;

        if (message.isRoomMaster)
        {
            RoomManager.Instance.CloseRoomFromServer(message.roomId);
        }
        else
        {
            RoomManager.Instance.LeaveRoomFromServer(_clients[message.messageOwnerId]);
        }

        sender.roomId = 0;

        message.succesful = true;

        SendMessageToClients(message);
    }

    private void HandleReadyInTheRoomMessage(NetworkMessage data)
    {
        var message = data as ReadyInTheRoom;
    }

    private void HandleStartGameMessage(NetworkMessage data)
    {
        var message = data as StartGame;

        var client = _clients[message.messageOwnerId];

        if (!client.isRoomMaster)
        {
            message.succesful = false;

            SendMessageToClient(client, message);

            return;
        }

        RoomManager.Instance.StartGameFromServer(message.roomId);

        message.succesful = true;

        var clients = RoomManager.Instance.GetClientsInfoByRoomId(message.roomId);

        _gameManager.InitGame(clients);

        // Create Net Ids for players
        message.playerIds = new();
        message.netIds = new();

        foreach (var c in clients)
        {
            message.playerIds.Add(c.id);
            message.netIds.Add(GetNextNetID());
        }

        SendMessageToClients(message);
    }

    private void HandleKickOutRoomMessage(NetworkMessage data)
    {
        var message = data as KickOutRoom;
    }

    public void HandleUpdatePlayerMovement(NetworkMessage data)
    {
        var message = data as UpdatePlayerMovement;

        message.succesful = true;

        SendMessageToClients(message);
    }
}
