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
    private bool _requestLeaveTheRoom = false;

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

        if (_requestLeaveTheRoom)
            LeaveTheRoom();
    }

    public void JoinRoomUDP()
    {
        if (_nameInput.text == "")
            return;

        _startPanel.SetActive(false);
        _clientPanel.SetActive(true);
        _connected = true;

        Thread thread = new(ClientHandler);

        thread.Start();
    }

    public void LeaveTheRoom()
    {
        _startPanel.SetActive(true);
        _clientPanel.SetActive(false);
        _connected = false;
        _requestLeaveTheRoom = false;
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

        Debug.Log("Leave the room");

        _socket = null;
    }

    private void OnApplicationQuit()
    {
        LeaveTheRoom();
    }

    public void SendMessageToServer()
    {
        if (_messageInput.text == "")
            return;

        string messageToSend = _nameInput.text + ": " + _messageInput.text;

        byte[] data = Encoding.ASCII.GetBytes(messageToSend);

        try
        {
            // Send data to server
            _socket.SendTo(data, 0, data.Length, SocketFlags.None, _serverEndPoint);

            Debug.Log("Send message to Server");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }

        // Reset InputBox
        _messageInput.text = "";
    }

    private void ClientHandler()
    {
        string serverIP = _ipInput.text;
        int serverPort = 8888;

        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            _serverEndPoint = new(IPAddress.Parse(serverIP), serverPort);

            string messageToSend = "JOINUDPROOM";
            byte[] data = Encoding.ASCII.GetBytes(messageToSend);

            // Send data to server
            _socket.SendTo(data, 0, data.Length, SocketFlags.None, _serverEndPoint);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);

            _requestLeaveTheRoom = true;
            return;
        }

        Debug.Log("Joined to the room");

        _connected = true;

        // Start to listen messages
        ReciveMessage();
    }

    private void ReciveMessage()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        Debug.Log("Start receive message");

        while (_connected)
        {
            try
            {
                bytesRead = _socket.Receive(buffer);

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (receivedMessage == "SERVERCLOSED")
                {
                    _requestLeaveTheRoom = true;
                    return;
                }

                Debug.Log("Message recived: " + receivedMessage + "\t" + "message length: " + bytesRead);

                lock (this)
                    _tempText?.Append("\n" + receivedMessage);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);

                _requestLeaveTheRoom = true;
            }
        }
    }
}
