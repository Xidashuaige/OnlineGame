﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    private bool _stillInRoom = false;
    private TcpListener _listener;
    [SerializeField] private InputField _inputField;

    [SerializeField] private GameObject _panel1;
    [SerializeField] private GameObject _panel2;
    public void CreateRoom()
    {
        _panel1.SetActive(false);
        _panel2.SetActive(true);
        _stillInRoom = true;

        StartCoroutine(ServerHandler());
    }

    public void CloseRoom()
    {
        _stillInRoom = false;
    }
    private IEnumerator ServerHandler()
    {
        string serverIP = "10.0.103.37";
        int serverPort = 888;

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
        
        /*
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
        */

        _listener.Stop();
        Debug.Log("Listener Closed");


        yield return null;
    }
}
