using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class UDP_Client : MonoBehaviour
{
    [Header("Strat Panel parameters")]
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private TMP_InputField _ipInput;

    [Space, Header("UPD Client Panel parameters")]
    [SerializeField] private TMP_InputField _messageInput;
    [SerializeField] private TMP_Text _messageBox;

    [Space, Header("Global parameters")]
    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _clientPanel;

    private readonly StringBuilder _tempText = new();

    // Socket parameters
    private bool _connected = false;
    private Socket _socket;
    private IPEndPoint _serverEndPoint;

    private void Update()
    {
        // Update Text box
        if (_tempText.Length > 0)
        {
            _messageBox.text += _tempText.ToString();

            // Reset temporary text
            lock (this)
                _tempText.Clear();
        }
    }

    public void JoinRoomUDP()
    {
        _startPanel.SetActive(false);
        _clientPanel.SetActive(true);

        Thread thread = new(ClientHandler);

        thread.Start();
    }

    public void LeaveTheRoom()
    {
        _startPanel.SetActive(true);
        _clientPanel.SetActive(false);
        _connected = false;
        _messageBox.text = "";

        try
        {
            string messageToSend = "LEAVEUDPROOM";
            byte[] data = Encoding.ASCII.GetBytes(messageToSend);

            // Send data to server
            _socket.SendTo(data, 0, data.Length, SocketFlags.None, _serverEndPoint);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }

        DebugManager.AddLog("Leave the room");
        Debug.Log("Leave the room");

        _socket = null;
    }

    private void OnApplicationQuit()
    {
        LeaveTheRoom();
    }

    public void SendMessageToServer()
    {
        string messageToSend = _nameInput.text + ": " + _messageInput.text;

        byte[] data = Encoding.ASCII.GetBytes(messageToSend);

        try
        {
            // Send data to server
            _socket.SendTo(data, 0, data.Length, SocketFlags.None, _serverEndPoint);

            // Reset InputBox
            _messageInput.text = "";

            // Add text to temporary text
            lock (this)
                _tempText.Append("\n" + messageToSend);

            DebugManager.AddLog("Send message to Server");
            Debug.Log("Send message to Server");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }
    }

    private void ClientHandler()
    {
        string serverIP = _ipInput.text;
        int serverPort = 8888;

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        _serverEndPoint = new(IPAddress.Parse(serverIP), serverPort);

        try
        {
            string messageToSend = "JOINUDPROOM";
            byte[] data = Encoding.ASCII.GetBytes(messageToSend);

            // Send data to server
            _socket.SendTo(data, 0, data.Length, SocketFlags.None, _serverEndPoint);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }

        DebugManager.AddLog("Joined to the room");
        Debug.Log("Joined to the room");

        // Start to listen messages
        ReciveMessage();
    }

    private void ReciveMessage()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (_connected)
        {
            try
            {
                bytesRead = _socket.Receive(buffer);

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (receivedMessage == "SERVERCLOSED")
                {
                    LeaveTheRoom();
                    return;
                }

                DebugManager.AddLog("Message recived num: " + bytesRead);
                Debug.Log("Message recived num: " + bytesRead);
                DebugManager.AddLog("Message recived: " + receivedMessage);
                Debug.Log("Message recived: " + receivedMessage);

                lock (this)
                    _tempText?.Append("\n" + receivedMessage);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);

                LeaveTheRoom();
            }
        }
    }
}
