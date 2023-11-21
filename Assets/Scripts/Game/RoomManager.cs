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
            RoomInfo newRoom = new(GetNextID(), message.roomMaster, message.maxUser);
            _roomPoolForServer.Add(newRoom);

            return newRoom;
        }
        return null;
    }

    public void CloseRoomFromServer()
    {

    }

    /*
    public bool CheckIfRoomAvaliable(uint roomID)
    {
        var roomIndex = _roomPoolForServer.FindIndex(room => room.ID == roomID);

        if (roomIndex > -1 && !_roomPoolForServer[roomIndex].IsFull)
            return true;

        return false;
    }
    */
    public void JoinRoomFromServer(JoinRoom message)
    {
        /*
        var roomIndex = _roomPoolForServer.FindIndex(room => room.ID == message.roomId);

        if (roomIndex > -1)
            _roomPoolForServer[roomIndex].JoinRoom(message.client);
        */
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
    public Room CreateRoom(ClientInfo roomMaster, uint roomId, int limitUser = 4)
    {
        Room room = GetNextRoom();

        if (room == null)
            return null;

        room.RoomInit(roomId, roomMaster, limitUser, _client.RequestJoinRoom);

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
    public void JoinRoom(JoinRoom message)
    {
        /*
        var roomIndex = _roomPoolForServer.FindIndex(room => room.ID == message.roomId);

        if (roomIndex > -1)
        {
            _roomPoolForServer[roomIndex].JoinRoom(message.client);

            for (int i = 0; message.clientsInTheRoom != null && i < message.clientsInTheRoom.Length; i++)
            {
                if (message.clientsInTheRoom[i].id == _client.ID)
                    continue;

                _roomPoolForServer[roomIndex].JoinRoom(message.clientsInTheRoom[i]);
            }
        }
        */
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
