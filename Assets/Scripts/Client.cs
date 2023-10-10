using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;

public class Client : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private TMP_InputField _ipInput;
    [SerializeField] private TMP_InputField _messageInput;

    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _clientPanel;


    // Socket parameters
    private bool _connected = false;
    private Socket _clientSocket;

    public void JoinRoom()
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
            _clientSocket.Shutdown(SocketShutdown.Both);
            _clientSocket.Close();
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
        }
    }

    private void OnApplicationQuit()
    {
        LeaveTheRoom();
    }

    public void SendMessageToServer()
    {
        string messageToSend = "Hello, Server!";

        byte[] data = Encoding.ASCII.GetBytes(messageToSend);

        try
        {
            _clientSocket.Send(data, data.Length, SocketFlags.None);

            Debug.Log("Send message to Server");
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }
    }

    private void ClientHandler()
    {
        string serverIP = "10.0.53.27";
        int serverPort = 8888;

        _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPEndPoint ipep = new(IPAddress.Parse(serverIP), serverPort);

        string messageToSend = "Hello, Server!";

        byte[] data = Encoding.ASCII.GetBytes(messageToSend);

        try
        {
            _clientSocket.Connect(ipep);

            Thread thread = new Thread(ReciveMessage);

            thread.Start();

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

                Debug.Log("Message recived num: " + bytesRead);
                Debug.Log("Message recived: " + receivedMessage);

            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);

                LeaveTheRoom();
            }
        }
    }
}
