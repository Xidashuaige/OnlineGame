using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public struct ClientForServer
{
    public ClientForServer(string name, uint id, IPEndPoint endPoint)
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
    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _roomPanel;
    [SerializeField] private GameObject _gamePanel;

    // Server parameters
    private RoomManager _roomManager;
    private List<ClientForServer> _clients;
    private uint _idGen = 0;
    private Dictionary<NetworkMessageType, Action<NetworkMessage>> _actionHandlers;

    // Socket parameters
    private bool _connecting = false;
    private Socket _socket;
    private const int _serverPort = 8888;
    private EndPoint _tempClientEndpoint;


    // Start is called before the first frame update
    void Start()
    {
        _clients = new();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CreateServer()
    {
        if (_connecting)
            return;

        Thread thread = new(StartServer);

        thread.Start();
    }

    private void StartServer()
    {
        if (_socket == null)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            _tempClientEndpoint = new IPEndPoint(IPAddress.Any, 0);
        }


        try
        {
            _socket.Bind(new IPEndPoint(IPAddress.Any, _serverPort));
            DebugManager.AddLog("Room created");
            Debug.Log("Room created!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
            DebugManager.AddLog(ex.Message);
            return;
        }

        // Start to listen messages
        ListenMessages();
    }

    private void ListenMessages()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (_connecting)
        {
            try
            {
                bytesRead = _socket.ReceiveFrom(buffer, ref _tempClientEndpoint);

                if (bytesRead == 0)
                    continue;

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                NetworkMessage message = JsonUtility.FromJson<NetworkMessage>(receivedMessage);

                if (message.type == NetworkMessageType.JoinServer)
                    message.endPoint = _tempClientEndpoint;

                ThreadPool.QueueUserWorkItem(new WaitCallback(HandleMessage), message);

                DebugManager.AddLog("Message recived: " + receivedMessage + "\t" + "message length: " + bytesRead);
                Debug.Log("Message recived: " + receivedMessage + "\t" + "message length: " + bytesRead);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);
                DebugManager.AddLog(ex.Message);
                return;
            }
        }
    }

    public void SendMessageToClients(NetworkMessage message)
    {
        byte[] data = message.GetBytes();

        foreach (var client in _clients)
        {
            // Send data to server, this function may not block the code
            _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(SendCallback), null);
        }
    }

    public void SendMessageToClient(ClientForServer client, NetworkMessage message)
    {
        byte[] data = message.GetBytes();

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
            Console.WriteLine(e.ToString());
        }
    }


    // -----------------------------------------------
    // -----------------------------------------------
    // ---------------------TOOL----------------------
    // -----------------------------------------------
    // -----------------------------------------------
    private uint GetNextID()
    {
        return _idGen++;
    }


    // -----------------------------------------------
    // -----------------------------------------------
    // ------------HANDLE PLAYER MESSAGES-------------
    // -----------------------------------------------
    // -----------------------------------------------
    private void HandleMessage(object messageObj)
    {
        NetworkMessage message = messageObj as NetworkMessage;

        message.succesful = true;

        _actionHandlers[message.type].Invoke(message);
    }

    private void HandleJoinServerMessage(JoinServer message)
    {
        ClientForServer client = new(message.name, GetNextID(), (IPEndPoint)_tempClientEndpoint);

        _clients.Add(client);

        message.id = client.id;

        SendMessageToClient(client, message);
    }

    private void HandleLeaveServerMessage(LeaveServer message)
    {

    }

    private void HandleCreateRoomMessage(CreateRoom message)
    {

    }

    private void HandleJoinRoomMessage(JoinRoom message)
    {

    }

    private void HandleLeaveRoomMessage(LeaveRoom message)
    {

    }

    private void HandleReadyInTheRoomMessage(ReadyInTheRoom message)
    {

    }

    private void HandleStartGameMessage(StartGame message)
    {

    }

    private void HandleKickOutRoomMessage(KickOutRoom message)
    {

    }
}
