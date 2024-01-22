using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance = null;

    [Header("Server")]
    [SerializeField] private int _maxRooms = 20;
    private readonly List<RoomInfo> _roomPoolForServer = new();
    private uint _idGen = 0;
    public bool CanCreateMore { get => _roomPoolForServer.Count >= _maxRooms ? false : true; }

    [Space, Header("Client")]
    [SerializeField] private RoomUIController _roomUIController;
    [SerializeField] private GameObject _roomPrefab;
    [SerializeField] private GameObject _roomParent;
    private readonly List<Room> _roomPoolForClient = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject.transform.parent);
        }
    }

    private void Start()
    {
        // Init for client
        if (_roomUIController == null)
            _roomUIController = GameObject.FindWithTag("RoomController").GetComponent<RoomUIController>();

        _roomUIController.InitRoomController();

        for (int i = 0; i < _maxRooms; i++)
        {
            var room = Instantiate(_roomPrefab);
            room.SetActive(false);
            room.transform.SetParent(_roomParent.transform);
            room.transform.localScale = Vector3.one;
            _roomPoolForClient.Add(room.GetComponent<Room>());
        }

        Client.Instance.onActionHandlered[NetworkMessageType.JoinServer] += OnJoinServerFromClient;
        Client.Instance.onActionHandlered[NetworkMessageType.CreateRoom] += OnCreateRoomFromClient;
        Client.Instance.onActionHandlered[NetworkMessageType.JoinRoom] += OnJoinRoomFromClient;
        Client.Instance.onActionHandlered[NetworkMessageType.LeaveRoom] += OnLeaveRoomFromClient;
        Client.Instance.onActionHandlered[NetworkMessageType.StartGame] += OnStartGameFromClient;
        Client.Instance.onActionHandlered[NetworkMessageType.CloseRoom] += OnCloseRoomFromClient;
    }

    private void OnApplicationQuit()
    {
        _roomPoolForServer.Clear();
        _roomPoolForClient.Clear();
    }

    // -----------------------------------------------
    // -----------------------------------------------
    // ---------------SERVER FUNCTIONS----------------
    // -----------------------------------------------
    // -----------------------------------------------

    #region Server Functions

    public RoomInfo CreateRoomFromServer(CreateRoom message)
    {
        if (CanCreateMore)
        {
            RoomInfo newRoom = new(GetNextID(), null, message.maxUser);
            _roomPoolForServer.Add(newRoom);

            return newRoom;
        }
        return null;
    }

    public bool CheckIfRoomAvaliable(uint roomID)
    {
        var roomIndex = _roomPoolForServer.FindIndex(room => room.id == roomID);

        if (roomIndex >= 0 && !_roomPoolForServer[roomIndex].IsFull)
            return true;

        return false;
    }

    public List<RoomInfo> GetActiveRooms()
    {
        return _roomPoolForServer;
    }

    public bool JoinRoomFromServer(JoinRoom message)
    {
        var roomIndex = _roomPoolForServer.FindIndex(room => room.id == message.roomId);

        if (roomIndex < 0)
        {
            Debug.Log("Server: room " + message.roomId + " is not found");
            message.roomId = 0;
            return false;
        }

        var room = _roomPoolForServer[roomIndex];

        if (room.IsFull)
        {
            Debug.Log("Server: Room " + message.roomId + " is already full");
            return false;
        }

        var client = message.client;

        Debug.Log("Server: room " + room.id + " has " + room.clients?.Count + " clients");

        if (room.clients?.Count == 0)
        {
            Debug.Log("Server: client (" + client.name + ") will join room with room master role");
            client.isRoomMaster = true;
        }
        else
        {
            Debug.Log("Server : get other players in the same room");
            // Get clients for other players
            message.clientsInTheRoom = room.clients;
        }

        client.roomId = room.id;

        room.clients.Add(client);

        room.state = room.clients.Count >= room.limitUsers ? RoomState.Full : RoomState.NotFull;

        // First player in the room will be room master
        message.roomMasterId = room.clients[0].id;

        return true;
    }

    private uint GetNextID()
    {
        return ++_idGen;
    }

    public void LeaveRoomFromServer(ClientInfo user)
    {
        var roomIdex = _roomPoolForServer.FindIndex(room => room.id == user.roomId);

        if (roomIdex == -1)
            return;

        var room = _roomPoolForServer[roomIdex];

        user.roomId = 0;

        room.clients.Remove(user);

        if (user.isRoomMaster)
        {
            foreach (var client in room.clients)
                client.roomId = 0;       

            room.clients.Clear();

            _roomPoolForServer.Remove(room);
        }
    }

    public void CloseRoomFromServer(uint roomId)
    {
        var roomIdex = _roomPoolForServer.FindIndex(room => room.id == roomId);

        if (roomIdex == -1)
            return;

        foreach (var client in _roomPoolForServer[roomIdex].clients)
        {
            client.isRoomMaster = false;
            client.roomId = 0;
        }

        _roomPoolForServer.Remove(_roomPoolForServer[roomIdex]);
    }

    public void StartGameFromServer(uint roomId)
    {
        var roomIdex = _roomPoolForServer.FindIndex(room => room.id == roomId);

        if (roomIdex == -1)
            return;

        var room = _roomPoolForServer[roomIdex];

        room.state = RoomState.InGame;
    }

    public bool KillPlayerFromServer(uint roomId, uint netId)
    {
        var roomIdex = _roomPoolForServer.FindIndex(room => room.id == roomId);

        if (roomIdex == -1)
            return false;

        var room = _roomPoolForServer[roomIdex];

        if (room.deadPlayers.Contains(netId) || room.state != RoomState.InGame)
            return false;

        room.deadPlayers.Add(netId);

        if (room.deadPlayers.Count >= room.clients.Count - 1)
        {
            room.deadPlayers.Clear();
            room.state = room.IsFull ? RoomState.Full : RoomState.NotFull;
            return true;
        }

        return false;
    }

    public List<ClientInfo> GetClientsInfoByRoomId(uint roomId)
    {
        var roomIdex = _roomPoolForServer.FindIndex(room => room.id == roomId);

        if (roomIdex == -1)
            return null;

        var room = _roomPoolForServer[roomIdex];

        var clients = room.clients;

        return clients;
    }

    #endregion
    // -----------------------------------------------
    // -----------------------------------------------
    // ------------- CLIENT FUNCTIONS-----------------
    // -----------------------------------------------
    // -----------------------------------------------

    #region Client Functions

    public void OnJoinServerFromClient(NetworkMessage data)
    {
        var message = data as JoinServer;

        if (message.currentPlayers == null)
            return;

        for (int i = 0; i < message.currentPlayers.Count; i++)
        {
            Room room = GetNextRoom();

            if (room == null)
                return;

            room.RoomInit(message.roomsId[i], message.maxPlayers[i], Client.Instance.RequestJoinRoom, message.currentPlayers[i], message.roomState[i]);
        }
    }

    public void OnCreateRoomFromClient(NetworkMessage data)
    {
        var message = data as CreateRoom;

        Room room = GetNextRoom();

        if (room == null)
            return;

        room.RoomInit(message.roomId, message.maxUser, Client.Instance.RequestJoinRoom);
    }

    private void CloseRoomFromClient(uint roomId)
    {
        var roomIndex = _roomPoolForClient.FindIndex(room => room.ID == roomId);

        if (roomIndex < 0)
            return;

        _roomPoolForClient[roomIndex].CloseRoom();
    }

    private void LeaveRoomFromClient(uint roomId)
    {
        var roomIndex = _roomPoolForClient.FindIndex(room => room.ID == roomId);

        Debug.LogWarning("Leave Room!");

        if (roomIndex < 0)
            return;

        _roomPoolForClient[roomIndex].LeaveRoom();
    }

    public void OnJoinRoomFromClient(NetworkMessage data)
    {
        var message = data as JoinRoom;

        var roomIndex = _roomPoolForClient.FindIndex(room => room.ID == message.roomId);

        if (roomIndex < 0)
        {
            Debug.Log("Client: doesn't find room " + message.roomId + " in the client (" + Client.Instance.Name + ")");
            return;
        }

        var room = _roomPoolForClient[roomIndex];

        if (!room.JoinRoom())
        {
            Debug.Log("Client: room " + message.roomId + " is full in the client");
            return;
        }
    }

    public void OnStartGameFromClient(NetworkMessage data)
    {
        var message = data as StartGame;

        var roomIndex = _roomPoolForClient.FindIndex(room => room.ID == message.roomId);

        if (roomIndex < 0)
            return;

        var room = _roomPoolForClient[roomIndex];
    }

    public void OnCloseRoomFromClient(NetworkMessage data)
    {
        var message = data as JoinRoom;

        CloseRoomFromClient(message.roomId);
    }

    private Room GetNextRoom()
    {
        for (int i = 0; i < _roomPoolForClient.Count; i++)
        {
            if (!_roomPoolForClient[i].gameObject.activeInHierarchy)
                return _roomPoolForClient[i];
        }
        return null;
    }

    private void OnLeaveRoomFromClient(NetworkMessage data)
    {
        var message = data as LeaveRoom;

        if (message.isRoomMaster)
            CloseRoomFromClient(message.roomId);
        else
            LeaveRoomFromClient(message.roomId);
    }

    #endregion
}
