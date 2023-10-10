using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
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
            Debug.LogException(ex);
        }
    }

    private void OnApplicationQuit()
    {
        try
        {
            _clientSocket.Shutdown(SocketShutdown.Both);
            _clientSocket.Close();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public void SendMessageToServer()
    {
        string messageToSend = "Hello, Server!";

        byte[] data = Encoding.ASCII.GetBytes(messageToSend);

        try
        {
            _clientSocket.Send(data, data.Length, SocketFlags.None);
            Debug.Log("Send to Server");
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }

    private void ClientHandler()
    {
        string serverIP = "10.0.103.32";
        int serverPort = 8888;

        _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPEndPoint ipep = new(IPAddress.Parse(serverIP), serverPort);

        string messageToSend = "Hello, Server!";

        byte[] data = Encoding.ASCII.GetBytes(messageToSend);

        try
        {
            _clientSocket.Connect(ipep);
            Debug.Log("Connected to Server");

            _clientSocket.Send(data, data.Length, SocketFlags.None);
            Debug.Log("Send to Server");
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }
}
