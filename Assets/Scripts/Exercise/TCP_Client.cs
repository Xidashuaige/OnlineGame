using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class TCP_Client : MonoBehaviour
{
    [Header("Strat Panel parameters")]
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private TMP_InputField _ipInput;

    [Space, Header("TCP Client Panel parameters")]
    [SerializeField] private TMP_InputField _messageInput;
    [SerializeField] private TMP_Text _messageBox;

    [Space, Header("Global parameters")]
    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _clientPanel;

    private readonly StringBuilder _tempText = new();
    private bool _requestLeaveRoom = false;


    // Socket parameters
    private bool _connected = false;
    private Socket _socket;

    private void Update()
    {
        if (_tempText.Length > 0)
        {
            _messageBox.text += _tempText.ToString();

            lock (this)
                _tempText.Clear();
        }

        if (_requestLeaveRoom)
            LeaveTheRoom();
    }

    public void JoinRoomTCP()
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
        _requestLeaveRoom = false;
        _messageBox.text = "";

        try
        {
            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
            //DebugManager.AddLog(ex.Message);
        }

        Debug.Log("Leave the room");
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
            _socket.Send(data);

            Debug.Log("Send message to Server");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }

        _messageInput.text = "";
    }

    private void ClientHandler()
    {
        string serverIP = _ipInput.text;
        int serverPort = 8888;

        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint ipep = new(IPAddress.Parse(serverIP), serverPort);

            string messageToSend = "Hello, Server!";

            byte[] data = Encoding.ASCII.GetBytes(messageToSend);

            _socket.Connect(ipep);

            Thread thread = new(ReceiveMessage);

            thread.Start();

            Debug.Log("Connected to Server");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);

            _requestLeaveRoom = true;
        }
    }

    private void ReceiveMessage()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        Debug.Log("Start receive message");

        while (_connected)
        {
            try
            {
                bytesRead = _socket.Receive(buffer);

                if (bytesRead == 0)
                {
                    Debug.Log("Server has been disconnect");
                    _requestLeaveRoom = true;
                    return;
                }

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                Debug.Log("Message recived: " + receivedMessage + "\t" + "message length: " + bytesRead);

                lock (this)
                    _tempText.Append("\n" + receivedMessage);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);
                _requestLeaveRoom = true;
                return;
            }
        }
    }
}
