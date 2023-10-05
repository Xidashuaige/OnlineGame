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
    private TcpListener _listener;
    private Socket _serverSocket;
    private Socket _clientSocket;
    public void CreateRoom()
    {
        _panel1.SetActive(false);
        _panel2.SetActive(true);
        _connected = true;

        StartCoroutine(ServerHandler());
    }

    public void CloseRoom()
    {
        _connected = false;
    }
    private IEnumerator ServerHandler()
    {
        int serverPort = 8888;

        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPEndPoint ipep = new(IPAddress.Any, serverPort);

        _serverSocket.Bind(ipep);

        _serverSocket.Listen(5);

        while (_connected)
        {
            try
            {
                _clientSocket = _serverSocket.Accept();

                ParameterizedThreadStart receiveMethod = new(ReciveMessage);

                Thread thread = new(receiveMethod);

                thread.Start(_clientSocket);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }

        _serverSocket.Close();
        Debug.Log("Server closed");

        yield break;

        /*
        Debug.Log("Creating Listener");

        _listener = new TcpListener(IPAddress.Parse(serverIP), serverPort);

        Debug.Log("Listener Created");

        try
        {
            _listener.Start();
            Debug.Log("Server connected，Waitin for client connection...");
        }
        catch (Exception e)
        {
            Debug.Log("Error: " + e.Message);
        }

        // Waiting for client connection
        Debug.Log("Waiting for client connection");
        TcpClient client = _listener.AcceptTcpClient();
        Debug.Log("Client connected");

        // Get client stream
        NetworkStream stream = client.GetStream();

        // Buffer to save information
        byte[] buffer = new byte[1024];
        int bytesRead;

        
        while (_stillInRoom)
        {
            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Message from" + "a" + "：" + receivedMessage);

                    // resend message to client
                    string responseMessage = "Message Recived：" + receivedMessage;
                    byte[] responseData = Encoding.ASCII.GetBytes(responseMessage);
                    stream.Write(responseData, 0, responseData.Length);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("CLient Error: " + e.Message);
            }
        }

        // Close client
        client.Close();
        Console.WriteLine("Client broke");
       

        _listener.Stop();
        Debug.Log("Listener Closed");
       
        yield return null;

        */
    }

    private void ReciveMessage(object client)
    {
        Socket myClient = client as Socket;

        byte[] buffer = new byte[1024];
        int bytesRead;

        while (_connected)
        {
            bytesRead = myClient.Receive(buffer);

            string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            Debug.Log("Message recived: " + receivedMessage);
        }

        myClient.Close();
        Debug.Log("Client Closed");
    }
}
