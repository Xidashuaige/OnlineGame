using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class Client : MonoBehaviour
{
    [SerializeField] private PanelManager _panelManager;

    // Unity paramaters
    [SerializeField] private TMP_InputField _ipInput;
    [SerializeField] private TMP_InputField _nameInput;

    // Clients paramaters

    private IPEndPoint _serverEndPoint;

    private Socket _socket;

    private bool _connecting;

    public bool host = false;

    private const int SERVER_PORT = 8888;

    public uint ID { get => _id; }

    private uint _id;

    private Dictionary<NetworkMessageType, Action<NetworkMessage>> _actionHandlers = new();

    private Dictionary<NetworkMessageType, Action<bool>> _actionSuccessful = new();

    private readonly object _lock = new();

    private void Start()
    {
        // Init Handle events actions
        _actionHandlers[NetworkMessageType.Heartbeat] = HandleHeartBeatMessage;
        _actionHandlers[NetworkMessageType.StartGame] = HandleStartGameMessage;
        _actionHandlers[NetworkMessageType.ReadyInTheRoom] = HandleReadyInTheRoomMessage;
        _actionHandlers[NetworkMessageType.KickOutRoom] = HandleKickOutRoomMessage;
        _actionHandlers[NetworkMessageType.CreateRoom] = HandleCreateRoomMessage;
        _actionHandlers[NetworkMessageType.JoinRoom] = HandleJoinRoomMessage;
        _actionHandlers[NetworkMessageType.JoinServer] = HandleJoinServerMessage;
        _actionHandlers[NetworkMessageType.LeaveRoom] = HandleLeaveRoomMessage;
        _actionHandlers[NetworkMessageType.LeaveServer] = HandleLeaveServerMessage;

        // Init successful events actions
        _actionSuccessful[NetworkMessageType.Heartbeat] = WhenHeartBeatHasSent;
        _actionSuccessful[NetworkMessageType.StartGame] = WhenStartGameHasSent;
        _actionSuccessful[NetworkMessageType.ReadyInTheRoom] = WhenReadyInTheRoomHasSent;
        _actionSuccessful[NetworkMessageType.KickOutRoom] = WhenKickOutRoomHasSent;
        _actionSuccessful[NetworkMessageType.CreateRoom] = WhenCreateRoomHasSent;
        _actionSuccessful[NetworkMessageType.JoinRoom] = WhenJoinRoomHasSent;
        _actionSuccessful[NetworkMessageType.JoinServer] = WhenJoinServerHasSent;
        _actionSuccessful[NetworkMessageType.LeaveRoom] = WhenLeaveRoomHasSent;
        _actionSuccessful[NetworkMessageType.LeaveServer] = WhenLeaveServerHasSent;
    }

    private void OnApplicationQuit()
    {
        _connecting = false;

        _actionHandlers.Clear();
        _actionHandlers = null;

        _actionSuccessful.Clear();
        _actionSuccessful = null;

        _socket?.Dispose();
        _socket = null;
    }

    public void RequestJoinToServer()
    {
        if (_connecting)
            return;

        if (_nameInput.text == "")
        {
            Debug.Log("name null");

            if (_nameInput.TryGetComponent<UnityEngine.UI.Image>(out var img))
                StartCoroutine(InputFlashRed(img));

            return;
        }

        if (_ipInput.text == "")
        {
            Debug.Log("ip null");

            if (_ipInput.TryGetComponent<UnityEngine.UI.Image>(out var img))
                StartCoroutine(InputFlashRed(img));

            return;
        }

        if (_socket == null)
        {
            try
            {
                // Create socket and and serverEndPoint
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                _serverEndPoint = new(IPAddress.Parse(_ipInput.text), SERVER_PORT);
            }
            catch (Exception e)
            {
                _socket?.Dispose();
                _socket = null;

                Debug.Log(e);
                DebugManager.AddLog(e.ToString());
                return;
            }
        }

        _connecting = true;

        // Start to listen messages from server
        Thread thread = new(ListenMessages);

        thread.Start();

        // Try to connect to server
        var messagePackage = NetworkPackage.CreateJoinServerRequest(_nameInput.text);

        SendMessageToServer(messagePackage);
    }

    public void RequestCloseServer()
    {
        CloseServer message = new(ID);

        //SendMessageToServer(message);
    }

    // Utils
    private IEnumerator InputFlashRed(UnityEngine.UI.Image img)
    {
        float fadeSpeed = 0.02f;

        for (float t = 0.0f; t < 1.0f; t += fadeSpeed)
        {
            img.color = Color.Lerp(Color.white, Color.red, t);

            yield return null;
        }

        for (float t = 0.0f; t < 1.0f; t += fadeSpeed)
        {
            img.color = Color.Lerp(Color.red, Color.white, t);

            yield return null;
        }
    }

    private void ListenMessages()
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

                if (!netWorkMessage.succesful || bytesRead <= 0)
                {
                    lock (_lock)
                        _connecting = false;

                    Debug.LogWarning("Message Receive with erro, disconnect from server");
                    DebugManager.AddLog("Message Receive with erro, disconnect from server");

                    break;
                }

                HandleMessage(netWorkMessage);

                DebugManager.AddLog("Message recived: " + receivedMessage + "\t" + "message length: " + bytesRead);
                Debug.Log("Message recived: " + receivedMessage + "\t" + "message length: " + bytesRead);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message);
                DebugManager.AddLog(ex.Message);

                lock (_lock)
                    _connecting = false;
            }
        }
    }

    public void SendMessageToServer(NetworkPackage messagePackage)
    {
        byte[] data = messagePackage.GetBytes();

        // Send data to server
        _socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, _serverEndPoint, new AsyncCallback(SendCallback), messagePackage.Type);
    }
    // After message sent
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            int bytesSent = _socket.EndSend(ar);
            DebugManager.AddLog("Send " + bytesSent + " bytes to Server");
            Debug.Log("Send " + bytesSent + " bytes to Server");

            _actionSuccessful[(NetworkMessageType)ar.AsyncState]?.Invoke(true);

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());

            _actionSuccessful[(NetworkMessageType)ar.AsyncState]?.Invoke(false);
        }
    }


    // -----------------------------------------------
    // -----------------------------------------------
    // -------WHEN RECEIVE MESSAGE FROM SERVER--------
    // -----------------------------------------------
    // -----------------------------------------------

    #region HandleMessages

    private void HandleMessage(NetworkMessage message)
    {
        _actionHandlers[message.type].Invoke(message);
    }

    private void HandleHeartBeatMessage(NetworkMessage data)
    {
        var message = data as HearthBeat;

        Debug.Log("HeartBeat");
        DebugManager.AddLog("HeartBeat");
    }

    private void HandleJoinServerMessage(NetworkMessage data)
    {
        var message = data as JoinServer;

        _id = message.id;

        Debug.Log("Join Server Successful");
        DebugManager.AddLog("Join Server Successful");
    }

    private void HandleLeaveServerMessage(NetworkMessage data)
    {
        var message = data as LeaveServer;

        host = false;
    }

    private void HandleCreateRoomMessage(NetworkMessage data)
    {
        var message = data as CreateRoom;
    }

    private void HandleJoinRoomMessage(NetworkMessage data)
    {
        var message = data as JoinRoom;
    }

    private void HandleLeaveRoomMessage(NetworkMessage data)
    {
        var message = data as LeaveRoom;
    }

    private void HandleReadyInTheRoomMessage(NetworkMessage data)
    {
        var message = data as ReadyInTheRoom;
    }

    private void HandleStartGameMessage(NetworkMessage data)
    {
        var message = data as StartGame;
    }

    private void HandleKickOutRoomMessage(NetworkMessage data)
    {
        var message = data as KickOutRoom;
    }

    private void HandleCloseServer(NetworkMessage data)
    {
        var message = data as CloseServer;

    }
    #endregion


    // -----------------------------------------------
    // -----------------------------------------------
    // ----------WHEN MESSAGE SENT SUCCESSFUL---------
    // -----------------------------------------------
    // -----------------------------------------------

    private void WhenHeartBeatHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }

    private void WhenJoinServerHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenLeaveServerHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenCreateRoomHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenJoinRoomHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenLeaveRoomHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenReadyInTheRoomHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenStartGameHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
    private void WhenKickOutRoomHasSent(bool successful)
    {
        if (successful)
        {

        }
        else
        {

        }
    }
}
