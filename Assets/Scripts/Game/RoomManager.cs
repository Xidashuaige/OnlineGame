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
    [SerializeField] private Client _client;
    private void Start()
    {
        if (_client == null)
            _client = GameObject.FindWithTag("Client").GetComponent<Client>();

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

    public void CloseRoomFromServer()
    {

    }


    public bool CheckIfRoomAvaliable(uint roomID)
    {
        var roomIndex = _roomPoolForServer.FindIndex(room => room.id == roomID);

        if (roomIndex >= 0 && !_roomPoolForServer[roomIndex].IsFull)
            return true;

        return false;
    }

    public bool JoinRoomFromServer(JoinRoom message)
    {
        var roomIndex = _roomPoolForServer.FindIndex(room => room.id == message.roomId);

        if (roomIndex < 0)
        {
            Debug.Log("Room is not found");
            return false;
        }

        var room = _roomPoolForServer[roomIndex];

        if (room.IsFull)
        {
            Debug.Log("Room is already full");
            return false;
        }

        var client = message.client;

        Debug.Log("Server: room " + room.id + " has " + room.clients?.Count + " clients");

        if (room.clients?.Count == 0)
        {
            Debug.Log("The player will join room with room master role");
            client.isRoomMaster = true;
        }
        else
        {
            Debug.Log("Get other players from server");
            // Get clients for other players
            message.clientsInTheRoom = room.clients;
        }

        client.roomId = room.id;

        room.clients.Add(client);

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
        /*
        var roomIdex = _roomPoolForServer.FindIndex(room => room.ID == user.room);

        _roomPoolForServer[roomIdex].LeaveRoom(user);

        user.isRoomMaster = false;

        user.room = 0;
        */
    }

    // -----------------------------------------------
    // -----------------------------------------------
    // ------------- CLIENT FUNCTIONS-----------------
    // -----------------------------------------------
    // -----------------------------------------------
    public Room CreateRoomFromClient(CreateRoom meesage)
    {
        Room room = GetNextRoom();

        if (room == null)
            return null;

        room.RoomInit(meesage.roomId, meesage.roomMaster, meesage.maxUser, _client.RequestJoinRoom);

        return room;
    }

    public void CloseRoom(uint roomId)
    {
        /*
        var roomIndex = _roomPoolForServer.FindIndex(room => room.ID == roomId);

        if (roomIndex < 0)
            return;

        _roomPoolForServer[roomIndex].CloseRoom();
        */
    }
    public bool JoinRoomFromClient(JoinRoom message)
    {
        var roomIndex = _roomPoolForClient.FindIndex(room => room.ID == message.roomId);

        if (roomIndex < 0)
        {
            Debug.Log("Do not find room " + message.roomId + " in the client (" + _client.Name + ")");
            return false;
        }

        var room = _roomPoolForClient[roomIndex];

        if (!room.JoinRoom(message.client))
        {
            Debug.Log("Room " + message.roomId + " is full in the client");
            return false;
        }

        for (int i = 0; message.client.id == _client.ID && message.clientsInTheRoom != null && i < message.clientsInTheRoom.Count; i++)
        {
            if (message.clientsInTheRoom[i].id == _client.ID)
                continue;

            if (!room.JoinRoom(message.clientsInTheRoom[i]))
            {
                Debug.Log("Room " + message.roomId + " is full in the client");
                return false;
            }
        }

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
