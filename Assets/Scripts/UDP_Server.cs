using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class UDP_Server : MonoBehaviour
{
    [Header("Strat Panel parameters")]
    [SerializeField] private TMP_InputField _nameInput;

    [Space, Header("UPD Client Panel parameters")]
    [SerializeField] private TMP_Text _messageBox;

    [Space, Header("Global parameters")]
    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _serverPanel;

    private readonly StringBuilder _tempText = new();

    // Socket parameters
    private bool _connected = false;
    private Socket _serverSocket;
    private List<IPEndPoint> _clienEndPoints;

    private void Start()
    {
        _clienEndPoints = new();
    }

    private void Update()
    {
        if (_tempText.Length > 0)
        {
            _messageBox.text += _tempText.ToString();

            lock (this)
                _tempText.Clear();
        }
    }

    private void OnApplicationQuit()
    {
        CloseRoom();
    }

    public void CreateRoomUDP()
    {
        _startPanel.SetActive(false);
        _serverPanel.SetActive(true);
        _connected = true;

        Thread thread = new(ServerHandler);

        thread.Start();
    }

    public void CloseRoom()
    {
        _connected = false;

        _startPanel.SetActive(true);
        _serverPanel.SetActive(false);
        _messageBox.text = "";

        try
        {
            foreach (var endPoint in _clienEndPoints)
            {
                byte[] data = Encoding.ASCII.GetBytes("SERVERCLOSED");
                _serverSocket.SendTo(data, endPoint);
            }

            _clienEndPoints.Clear();

            _serverSocket.Close();
            DebugManager.AddLog("Server closed");
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

        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        try
        {
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, serverPort));
            DebugManager.AddLog("Room created");
            Debug.Log("Room created!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning((ex.Message));
        }

        // Start to listen messages
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
                bytesRead = _serverSocket.ReceiveFrom(buffer, ref clientEndpoint);

                if (bytesRead == 0)
                    continue;

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (receivedMessage == "JOINUDPROOM")
                {
                    IPEndPoint clientIpEndpoint = clientEndpoint as IPEndPoint;
                    if (!_clienEndPoints.Contains(clientIpEndpoint))
                        _clienEndPoints.Add(clientIpEndpoint);

                    DebugManager.AddLog("Some one join the room: " + clientEndpoint);
                    Debug.Log("Some one join the room: " + clientEndpoint);

                    continue;
                }
                else if (receivedMessage == "LEAVEUDPROOM")
                {
                    IPEndPoint clientIpEndpoint = clientEndpoint as IPEndPoint;
                    if (_clienEndPoints.Contains(clientIpEndpoint))
                        _clienEndPoints.Remove(clientIpEndpoint);

                    DebugManager.AddLog("Some one leave the room: " + clientEndpoint);
                    Debug.Log("Some one leave the room: " + clientEndpoint);

                    continue;
                }

                DebugManager.AddLog("Message recived from: " + clientEndpoint);
                Debug.Log("Message recived from: " + clientEndpoint);
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

                _serverSocket.Close();

                return;
            }
        }
    }
}