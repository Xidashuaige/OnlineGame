using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
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

    [Space, Header("Server & Client")]
    [SerializeField] private Client _myClient;
    private void Start()
    {
        if (_myClient == null)
            _myClient = GameObject.FindWithTag("Client").GetComponent<Client>();

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

        user.isRoomMaster = false;

        user.roomId = 0;

        room.clients.Remove(user);

       // _roomPoolForServer[roomIdex]   
    }

    public void CloseRoomFromServer(uint roomId)
    {
        var roomIdex = _roomPoolForServer.FindIndex(room => room.id == roomId);

        if (roomIdex == -1)
            return;

        _roomPoolForServer.Remove(_roomPoolForServer[roomIdex]);
    }

    // -----------------------------------------------
    // -----------------------------------------------
    // ------------- CLIENT FUNCTIONS-----------------
    // -----------------------------------------------
    // -----------------------------------------------

    public void CreateRoomWhenJoinServer(JoinServer message)
    {
        if (message.currentPlayers == null)
            return;


        for (int i = 0; i < message.currentPlayers.Count; i++)
        {
            Room room = GetNextRoom();

            if (room == null)
                return;

            room.RoomInit(message.roomsId[i], message.maxPlayers[i], _myClient.RequestJoinRoom, message.currentPlayers[i], message.roomState[i]);
        }
    }

    public Room CreateRoomFromClient(CreateRoom message)
    {
        Room room = GetNextRoom();

        if (room == null)
            return null;

        room.RoomInit(message.roomId, message.maxUser, _myClient.RequestJoinRoom);

        return room;
    }

    public void CloseRoomFromClient(uint roomId)
    {
        var roomIndex = _roomPoolForClient.FindIndex(room => room.ID == roomId);

        if (roomIndex < 0)
            return;

        _roomPoolForClient[roomIndex].CloseRoom();
    }

    public void LeaveRoomFromClient(uint userId, uint roomId)
    {
        var roomIndex = _roomPoolForClient.FindIndex(room => room.ID == roomId);

        if (roomIndex < 0)
            return;

        _roomPoolForClient[roomIndex].LeaveRoom();
    }

    public bool JoinRoomFromClient(JoinRoom message)
    {
        var roomIndex = _roomPoolForClient.FindIndex(room => room.ID == message.roomId);

        if (roomIndex < 0)
        {
            Debug.Log("Client: doesn't find room " + message.roomId + " in the client (" + _myClient.Name + ")");
            return false;
        }

        var room = _roomPoolForClient[roomIndex];

        if (!room.JoinRoom())
        {
            Debug.Log("Client: room " + message.roomId + " is full in the client");
            return false;
        }

        /*
        for (int i = 0; message.messageOwnerId == _myClient.ID && message.clientsInTheRoom != null && i < message.clientsInTheRoom.Count; i++)
        {
            if (message.clientsInTheRoom[i].id == _myClient.ID)
                continue;

            if (!room.JoinRoom(message.clientsInTheRoom[i]))
            {
                Debug.Log("Room " + message.roomId + " is full in the client");
                return false;
            }
        }
        */

        return true;
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
}
