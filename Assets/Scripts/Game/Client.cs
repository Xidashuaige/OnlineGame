using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Client : MonoBehaviour
{
    public IPEndPoint EndPoint { get; }

    private IPEndPoint _serverEndPoint;

    private IPEndPoint _endPoint;

    private Socket _socket;

    //private User _user;

    private bool _connecting;

    private const int _serverPort = 8888;

    public void ConnectToServer()
    {

    }

    private void ReciveMessage()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        DebugManager.AddLog("Start receive message");
        Debug.Log("Start receive message");

        while (_connecting)
        {
            try
            {
                bytesRead = _socket.Receive(buffer);

                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                NetworkMessage netWorkMessage = JsonUtility.FromJson<NetworkMessage>(receivedMessage);

                HandleMessage(netWorkMessage);

                DebugManager.AddLog("Message recived: " + receivedMessage + "\t" + "message length: " + bytesRead);
                Debug.Log("Message recived: " + receivedMessage + "\t" + "message length: " + bytesRead);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);
                DebugManager.AddLog(ex.Message);
            }
        }
    }

    private void HandleMessage(NetworkMessage netWorkMessage)
    {
        switch (netWorkMessage.type)
        {

        }
    }

    public void SendToServer(NetworkMessage message)
    {
        byte[] data = Encoding.ASCII.GetBytes(JsonUtility.ToJson(message));

        // Send data to server
        _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, _serverEndPoint, new AsyncCallback(SendCallback), null);
    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            int bytesSent = _socket.EndSend(ar);
            DebugManager.AddLog("Send " + bytesSent + " bytes to Server");
            Debug.Log("Send " + bytesSent + " bytes to Server");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}
