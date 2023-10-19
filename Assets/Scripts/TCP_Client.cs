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

    [Space, Header("UPD Client Panel parameters")]
    [SerializeField] private TMP_InputField _messageInput;
    [SerializeField] private TMP_Text _messageBox;

    [Space, Header("Global parameters")]
    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _clientPanel;

    private readonly StringBuilder _tempText = new();

    // Socket parameters
    private bool _connected = false;
    private Socket _clientSocket;

    private void Update()
    {
        if (_tempText.Length > 0)
        {
            _messageBox.text += _tempText.ToString();

            lock (this)
                _tempText.Clear();
        }
    }

    public void JoinRoomTCP()
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

        try
        {
            _clientSocket?.Shutdown(SocketShutdown.Both);
            _clientSocket?.Close();
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
        }

        DebugManager.AddLog("Leave the room");
        Debug.Log("Leave the room");
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
            _clientSocket.Send(data, data.Length, SocketFlags.None);

            _messageInput.text = "";

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

        _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPEndPoint ipep = new(IPAddress.Parse(serverIP), serverPort);

        string messageToSend = "Hello, Server!";

        byte[] data = Encoding.ASCII.GetBytes(messageToSend);

        try
        {
            _clientSocket.Connect(ipep);

            Thread thread = new(ReciveMessage);

            thread.Start();

            DebugManager.AddLog("Connected to Server");
            Debug.Log("Connected to Server");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }
    }

    private void ReciveMessage()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (_connected)
        {
            try
            {
                bytesRead = _clientSocket.Receive(buffer);

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                DebugManager.AddLog("Message recived num: " + bytesRead); 
                Debug.Log("Message recived num: " + bytesRead);
                DebugManager.AddLog("Message recived: " + receivedMessage); 
                Debug.Log("Message recived: " + receivedMessage);

                lock (this)
                    _tempText.Append("\n" + receivedMessage);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);

                LeaveTheRoom();
            }
        }
    }
}
