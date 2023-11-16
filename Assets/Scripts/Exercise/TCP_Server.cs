using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class TCP_Server : MonoBehaviour
{
    [Header("Strat Panel parameters")]
    [SerializeField] private TMP_InputField _nameInput;

    [Space, Header("TCP Server Panel parameters")]
    [SerializeField] private TMP_InputField _messageInput;
    [SerializeField] private TMP_Text _messageBox;
    [SerializeField] private TMP_Text _ipAdress;

    [Space, Header("Global parameters")]
    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _serverPanel;

    private readonly StringBuilder _tempText = new();
    private bool _requestCloseRoom = false;

    // Socket parameters
    private bool _connected = false;
    private Socket _serverSocket;
    private List<Socket> _clientsSocket;

    private void Start()
    {
        _clientsSocket = new();
    }

    private void Update()
    {
        if (_tempText.Length > 0)
        {
            _messageBox.text += _tempText.ToString();

            lock (this)
                _tempText.Clear();
        }

        if (_requestCloseRoom)
            CloseRoom();
    }

    private void OnApplicationQuit()
    {
        CloseRoom();
    }

    public void SendMessageToClients()
    {
        if (_messageInput.text == "")
            return;

        string messageToSend = _nameInput.text + ": " + _messageInput.text;

        byte[] data = Encoding.ASCII.GetBytes(messageToSend);

        try
        {
            for (int i = 0; i < _clientsSocket.Count; i++)
            {
                _clientsSocket[i].Send(data);
            }

            lock (this)
                _tempText.Append(messageToSend + "\n");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }

        _messageInput.text = "";
    }

    private void ReSendMessageToClients(string message)
    {
        lock (this)
            _tempText.Append(message + "\n");

        try
        {
            for (int i = 0; i < _clientsSocket.Count; i++)
            {
                _clientsSocket[i].Send(Encoding.ASCII.GetBytes(message));
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }
    }

    public void CreateRoomTCP()
    {
        if (_nameInput.text == "")
            return;

        _startPanel.SetActive(false);
        _serverPanel.SetActive(true);
        _connected = true;
        
        GetIPAdress();

        Thread thread = new(ServerHandler);

        thread.Start();
    }

    public void CloseRoom()
    {
        _connected = false;

        _startPanel.SetActive(true);
        _serverPanel.SetActive(false);
        _requestCloseRoom = false;
        _messageBox.text = "";

        try
        {
            for (int i = 0; i < _clientsSocket.Count; i++)
            {
                _clientsSocket[i]?.Shutdown(SocketShutdown.Both);
                _clientsSocket[i]?.Close();
            }
            Debug.Log("Clients Closed");

            _serverSocket.Close();
            Debug.Log("Server closed");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }
    }

    private void ServerHandler()
    {
        int serverPort = 8888;

        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Connect socket
        try
        {
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, serverPort));

            _serverSocket.Listen(5);
            Debug.Log("Room created!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
            _requestCloseRoom = true;
            return;
        }

        while (_connected)
        {
            try
            {
                Socket clientSocket = _serverSocket.Accept();

                _clientsSocket.Add(clientSocket);

                ParameterizedThreadStart receiveMethod = new(ReceiveMessage);

                Thread thread = new(receiveMethod);

                thread.Start(clientSocket);

                Debug.Log("Some client connected!");
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);
                _requestCloseRoom = true;
                return;
            }
        }
    }

    private void ReceiveMessage(object clientObj)
    {
        Socket client = clientObj as Socket;

        byte[] buffer = new byte[1024];
        int bytesRead;

        while (_connected)
        {
            try
            {
                bytesRead = client.Receive(buffer);

                if (bytesRead == 0)
                {
                    Debug.Log("Someone leave the rooom");

                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    _clientsSocket.Remove(client);
                    return;
                }

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                Debug.Log("Message recived: " + receivedMessage + "\t" + "message length: " + bytesRead);

                ReSendMessageToClients(receivedMessage);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);

                _clientsSocket.Remove(client);
                return;
            }
        }
    }

    private void GetIPAdress()
    {
        IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());

        foreach (IPAddress ip in ipEntry.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                _ipAdress.text = ip.ToString();
                return;
            }
        }
    }

    public void CopyIPAdress()
    {
        GUIUtility.systemCopyBuffer = _ipAdress.text;
        Debug.Log("IP Adress Copied");
    }
}
