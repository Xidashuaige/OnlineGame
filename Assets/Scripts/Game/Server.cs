using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;


public enum NetWorkMessageFlag
{
    Test1,
    Test2,
    JoinRoom,
    JoinRoomCallBack,
    LeaveRoom,
}

public class JoinRoom : NetWorkMessage
{
    public User user;
}

[Serializable]
public class NetWorkMessage
{
    public NetWorkMessageFlag flag;
}

public class Server : MonoBehaviour
{
    [Space, Header("Global parameters")]
    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _roomPanel;
    [SerializeField] private GameObject _gamePanel;

    private List<Client> _clients;
    private RoomManager _roomManager;

    // Socket parameters
    private bool _connected = false;
    private Socket _socket;
    private const int _serverPort = 8888;

    private uint _idGen = 0;

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
        Thread thread = new(StartServer);

        thread.Start();
    }

    private void StartServer()
    {
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

        ReceiveMessage();
    }

    private void ReceiveMessage()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (_connected)
        {
            try
            {
                EndPoint clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
                bytesRead = _socket.ReceiveFrom(buffer, ref clientEndpoint);

                if (bytesRead == 0)
                    continue;

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                HandleMessage(JsonUtility.FromJson<NetWorkMessage>(receivedMessage));

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

    private void HandleMessage(NetWorkMessage message)
    {
        /*
        switch (message.flag)
        {
            case NetWorkMessageFlag.JoinRoom:
                JoinRoom joinRoom = message as JoinRoom;

                Client client = new Client();
                client.users

                if (!_clienEndPoints.Contains(clientIpEndpoint))
                    _clienEndPoints.Add(clientIpEndpoint);

                //DebugManager.AddLog("Some one join the room: " + clientEndpoint);
                //Debug.Log("Some one join the room: " + clientEndpoint);

                break;
            case NetWorkMessageFlag.LeaveRoom:

                IPEndPoint clientIpEndpoint = clientEndpoint as IPEndPoint;

                if (_clienEndPoints.Contains(clientIpEndpoint))
                    _clienEndPoints.Remove(clientIpEndpoint);

                DebugManager.AddLog("Some one leave the room: " + clientEndpoint);
                Debug.Log("Some one leave the room: " + clientEndpoint);

                break;
        }
        */
    }

    private void ListenForClients()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    private void OnConncted(User user)
    {
        user.Id = _idGen++;
    }

    public void SendToClients(string messageToSend)
    {
        byte[] data = Encoding.ASCII.GetBytes(messageToSend);

        foreach (var client in _clients)
        {
            // Send data to server
            _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.EndPoint, new AsyncCallback(SendCallback), null);
        }
    }

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
}
