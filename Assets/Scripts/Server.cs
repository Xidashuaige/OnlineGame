using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class Server : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _serverPanel;

    // Socket parameters
    private bool _connected = false;
    private Socket _serverSocket;

    private List<Socket> _clientsSocket;

    private void Start()
    {
        _clientsSocket = new List<Socket>();
    }

    public void CreateRoomTCP()
    {
        _startPanel.SetActive(false);
        _serverPanel.SetActive(true);
        _connected = true;

        Thread thread = new(ServerHandler);

        thread.Start();
    }

    private void OnApplicationQuit()
    {
        CloseRoom();
    }

    public void CloseRoom()
    {
        _connected = false;

        _startPanel.SetActive(true);
        _serverPanel.SetActive(false);

        try
        {
            for (int i = 0; i < _clientsSocket.Count; i++)
            {
                _clientsSocket[i]?.Shutdown(SocketShutdown.Both);
                _clientsSocket[i]?.Close();        
            }

            Debug.Log("Clients Closed");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }

        try
        {
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

        IPEndPoint ipep = new(IPAddress.Any, serverPort);

        try
        {
            _serverSocket.Bind(ipep);

            _serverSocket.Listen(5);
            Debug.Log("Room created!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning((ex.Message));
        }

        while (_connected)
        {
            try
            {
                Socket clientSocket = _serverSocket.Accept();

                _clientsSocket.Add(clientSocket);

                ParameterizedThreadStart receiveMethod = new(ReciveMessage);

                Thread thread = new(receiveMethod);

                thread.Start(clientSocket);

                Debug.Log("Some client connected!");
            }
            catch (Exception ex)
            {
                Debug.LogWarning((ex.Message));
            }
        }
    }

    private void ReciveMessage(object clientObj)
    {
        Socket client = clientObj as Socket;

        byte[] buffer = new byte[1024];
        int bytesRead;

        while (_connected)
        {
            try
            {
                bytesRead = client.Receive(buffer);

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (bytesRead == 0)
                {
                    Debug.Log("Someone leave the rooom");

                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    _clientsSocket.Remove(client);
                    return;
                }

                Debug.Log("Message recived num: " + bytesRead);
                Debug.Log("Message recived: " + receivedMessage);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);

                client.Shutdown(SocketShutdown.Both);
                client.Close();
                _clientsSocket.Remove(client);
                return;
            }
        }
    }
}
