using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class Client : MonoBehaviour
{
    // Unity paramaters
    [SerializeField] private TMP_InputField _ipInput;
    [SerializeField] private TMP_InputField _nameInput;

    // Clients paramaters
    public IPEndPoint EndPoint { get; }

    private IPEndPoint _serverEndPoint;

    private IPEndPoint _endPoint;

    private Socket _socket;

    private bool _connecting;

    private const int SERVER_PORT = 8888;

    private uint _id;

    private Dictionary<NetworkMessageType, Action<NetworkMessage>> _actionHandlers;

    private readonly object _lock = new();

    public void JoinToServer()
    {
        if (_connecting)
            return;

        if (_socket == null)
        {
            // Create socket and and serverEndPoint
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            _serverEndPoint = new(IPAddress.Parse(_ipInput.text), SERVER_PORT);
        }

        _connecting = true;

        // Try to connect to server
        JoinServer message = new(_nameInput.text);

        SendMessageToServer(message);

        // Start to listen messages from server
        Thread thread = new(ListenMessages);

        thread.Start();
    }

    private void ListenMessages()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        DebugManager.AddLog("Start receive message");
        Debug.Log("Start receive message");

        while (_connecting)
        {
            try
            {
                bytesRead = _socket.Receive(buffer);

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                NetworkMessage netWorkMessage = JsonUtility.FromJson<NetworkMessage>(receivedMessage);

                if(!netWorkMessage.succesful)
                {
                    lock (_lock)
                        _connecting = false;

                    Debug.LogWarning("Message Receive with erro, disconnect from server");
                    DebugManager.AddLog("Message Receive with erro, disconnect from server");

                    break;
                }

                HandleMessage(netWorkMessage);

                DebugManager.AddLog("Message recived: " + receivedMessage + "\t" + "message length: " + bytesRead);
                Debug.Log("Message recived: " + receivedMessage + "\t" + "message length: " + bytesRead);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);
                DebugManager.AddLog(ex.Message);

                lock (_lock)
                    _connecting = false;
            }
        }
    }

    public void SendMessageToServer(NetworkMessage message)
    {
        byte[] data = message.GetBytes();

        // Send data to server
        _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, _serverEndPoint, new AsyncCallback(SendCallback), null);
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

            lock (_lock)
                _connecting = false;
        }
    }


    // -----------------------------------------------
    // -----------------------------------------------
    // ------------HANDLE PLAYER MESSAGES-------------
    // -----------------------------------------------
    // -----------------------------------------------

    #region HandleMessages

    private void HandleMessage(NetworkMessage message)
    {
        _actionHandlers[message.type].Invoke(message);
    }

    private void HandleJoinServerMessage(JoinServer message)
    {
        _id = message.id;
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
    #endregion
}
