using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Client : MonoBehaviour, NetworkUtil
{
    private IPEndPoint _serverEndPoint;

    private IPEndPoint _endPoint;

    private Socket _socket;

    private const int _serverPort = 8888;

    public void Send(string messageToSend)
    {
        byte[] data = Encoding.ASCII.GetBytes(messageToSend);

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
