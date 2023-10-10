using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class Server : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private GameObject _panel1;
    [SerializeField] private GameObject _panel2;

    // Socket parameters
    private bool _connected = false;
    private Socket _serverSocket;
    private Socket _clientSocket;

    public void CreateRoomTCP()
    {
        _panel1.SetActive(false);
        _panel2.SetActive(true);
        _connected = true;

        Thread thread = new(ServerHandler);

        thread.Start();
    }

    public void CloseRoom()
    {
        _connected = false;

        _panel1.SetActive(true);
        _panel2.SetActive(false);

        try
        {
            _clientSocket?.Close();
            Debug.Log("Client Closed");

        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }

        try
        {
            _serverSocket.Close();
            Debug.Log("Server closed");
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
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
            Debug.Log(ex.Message);
        }

        while (_connected)
        {
            try
            {
                _clientSocket = _serverSocket.Accept();

                Thread thread = new(ReciveMessage);

                thread.Start();

                Debug.Log("Some client connected!");
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
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

                if (bytesRead == 0)
                {
                    _clientSocket.Close();
                    return;
                }

                Debug.Log("Message recived num: " + bytesRead);
                Debug.Log("Message recived: " + receivedMessage);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }

        _clientSocket.Close();
        Debug.Log("Client Closed");
    }
}
