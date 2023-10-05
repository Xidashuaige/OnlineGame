using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;

public class Client : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private GameObject _panel1;
    [SerializeField] private GameObject _panel2;

    // Socket parameters
    private bool _connected = false;
    private Socket _clientSocket;

    public void JoinRoom()
    {
        _panel1.SetActive(false);
        _panel2.SetActive(true);   
    }

    private IEnumerator ClientHandler()
    {
        string serverIP = "10.0.53.19";
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

        // Close client socket
        _clientSocket.Close();

        yield return null;
    }
}
