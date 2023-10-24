using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

interface NetworkUtil
{
    public void Send(string messageToSend);
}

public enum NetWorkMessageFlag
{
    User,
    Player,
}

[Serializable]
public class NetWorkMessage
{
    public NetWorkMessage flag;
}

public class Server : MonoBehaviour, NetworkUtil
{
    [Space, Header("Global parameters")]
    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _roomPanel;
    [SerializeField] private GameObject _gamePanel;

    private List<Client> _clients;
    private RoomManager _roomManager;

    // Socket parameters
    private bool _connected = false;
    private Socket _serverSocket;

    private uint _idGen = 0;

    // Start is called before the first frame update
    void Start()
    {
        _clients = new();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void CreateServer()
    {


        _roomManager = new();
    }

    private void OnConncted(User user)
    {
        user.Id = _idGen++;
    }

    public void Send(string messageToSend)
    {
        throw new NotImplementedException();
    }
}
