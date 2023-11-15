using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct ClientInServer
{
    public ClientInServer(string name, uint id, IPEndPoint endPoint)
    {
        this.name = name;
        this.id = id;
        this.endPoint = endPoint;
    }

    public string name;
    public uint id;
    public IPEndPoint endPoint;
}

public class Server : MonoBehaviour
{
    // Unity Objects
    [Space, Header("Global parameters")]
    [SerializeField] private PanelManager _panelManager;
    [SerializeField] private Button _startServerBtn;
    [SerializeField] private TMP_Text _ipAdress;

    // Server parameters
    private RoomManager _roomManager;
    private List<ClientInServer> _clients = new();
    private uint _idGen = 0;
    private Dictionary<NetworkMessageType, Action<NetworkMessage>> _actionHandlers = new();
    [SerializeField] private Client _myClient;

    // Socket parameters
    private bool _connecting = false;
    private Socket _socket;
    private const int _serverPort = 8888;
    private readonly object _lock = new();

    // Requests
    private bool _triggerStartServer = false;


    // Start is called before the first frame update
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
        _connecting = false;

        _socket?.Dispose();
        _socket = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (_triggerStartServer)
        {
            _ipAdress.text = GetIPAdress();

            // Change btn state
            var text = _startServerBtn.GetComponentInChildren<TMP_Text>();

            _startServerBtn.onClick.RemoveAllListeners();

            _startServerBtn.onClick.AddListener(_myClient.RequestCloseServer);

            if (text != null)
                text.text = "Close Server";

            // Change my client to server host
            _myClient.host = true;

            _triggerStartServer = false;
        }
    }

    public void CreateServer()
    {
        if (_connecting)
            return;

        Thread thread = new(StartServer);

        thread.Start();
    }

    public void CloseServer(CloseServer message)
    {
        SendMessageToClients(message);

        _startServerBtn.onClick.RemoveAllListeners();

        _startServerBtn.onClick.AddListener(CreateServer);

        var text = _startServerBtn.GetComponentInChildren<TMP_Text>();

        if (text != null)
            text.text = "Start Server";

        lock (_lock)
            _connecting = false;
    }

    private void StartServer()
    {
        if (_socket == null)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                _socket.Bind(new IPEndPoint(IPAddress.Any, _serverPort));

                DebugManager.AddLog("Server Start!");
                Debug.Log("Server Start!");
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);
                DebugManager.AddLog(ex.Message);
                return;
            }
        }

        // When start server successful
        _triggerStartServer = true;

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

                NetworkMessage message = NetworkPackage.GetDataFromBytes(buffer);

                if (message.type == NetworkMessageType.JoinServer)
                    message.endPoint = _lastEndPoint;

                ThreadPool.QueueUserWorkItem(new WaitCallback(HandleMessage), message);

                DebugManager.AddLog("Message recived from client: " + message + "\t" + "message length: " + bytesRead);
                Debug.Log("Message recived from client: " + message + "\t" + "message length: " + bytesRead);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);
                DebugManager.AddLog(ex.Message);
                buffer = null;
                _connecting = false;
                return;
            }
        }
    }

    public void SendMessageToClients(NetworkMessage message)
    {
        Debug.Log("Send message : " + message.type);

        byte[] data = message.GetBytes();

        foreach (var client in _clients)
        {
            // Send data to server, this function may not block the code
            _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(SendCallback), null);
        }
    }

    public void SendMessageToClient(ClientInServer client, NetworkMessage message)
    {
        Debug.Log("Send messages : " + message.type + " to client : " + client.name);

        NetworkPackage package = new(message.type, message.GetBytes());

        byte[] data = package.GetBytes();

        // Send data to server, this function may not block the code
        _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(SendCallback), null);
    }

    // After message sent
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            int bytesSent = _socket.EndSend(ar);
            DebugManager.AddLog("Send " + bytesSent + " bytes to Server");
            Debug.Log("Send " + bytesSent + " bytes to Server");
        }
        catch (Exception e)
        {
            Debug.Log(e);
            DebugManager.AddLog(e.ToString());
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
        GUIUtility.systemCopyBuffer = _ipAdress.text;
        Debug.Log("IP Adress Copied");
        DebugManager.AddLog("IP Adress Copied");
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

        ClientInServer client = new(message.name, GetNextID(), message.endPoint as IPEndPoint);

        _clients.Add(client);

        message.id = client.id;

        SendMessageToClient(client, message);
    }

    private void HandleLeaveServerMessage(NetworkMessage data)
    {
        var message = data as LeaveServer;
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

    private void HandleCloseServer(NetworkMessage data)
    {
        var message = data as CloseServer;

        if (message.userId == _myClient.ID)
            CloseServer(message);
    }
}
