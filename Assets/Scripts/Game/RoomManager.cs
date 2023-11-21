using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private int _maxRooms = 20;
    [SerializeField] private GameObject _roomPrefab;
    [SerializeField] private GameObject _roomParent;
    [SerializeField] private Client _client;
    [SerializeField] private RoomController _roomController;

    // Client use
    private readonly List<Room> _roomPool = new();

    // Server use
    private int _roomCreated = 0;
    private uint _idGen = 0;
    public bool CanCreateMore { get => _roomCreated >= _maxRooms ? false : true; }

    private void Start()
    {
        if (_client == null)
            _client = GameObject.FindWithTag("Client").GetComponent<Client>();

        if (_roomController == null)
            _roomController = GameObject.FindWithTag("RoomController").GetComponent<RoomController>();

        _roomController.InitRoomController();

        for (int i = 0; i < _maxRooms; i++)
        {
            var room = Instantiate(_roomPrefab);
            room.SetActive(false);
            room.transform.SetParent(_roomParent.transform);
            room.transform.localScale = Vector3.one;
            _roomPool.Add(room.GetComponent<Room>());
        }
    }

    private void OnApplicationQuit()
    {
        _roomPool.Clear();
    }

    // -----------------------------------------------
    // -----------------------------------------------
    // ---------------SERVER FUNCTIONS----------------
    // -----------------------------------------------
    // -----------------------------------------------
    public uint CreateRoomFromServer()
    {
        if (CanCreateMore)
        {
            _roomCreated++;

            //var newRoom = CreateRoom(roomMaster, GetNextID(), maxUser);

            return GetNextID();
        }
        return 0;
    }

    public bool CheckIfRoomAvaliable(uint roomID)
    {
        var roomIndex = _roomPool.FindIndex(room => room.ID == roomID);

        if (roomIndex > -1 && !_roomPool[roomIndex].IsFull)
            return true;

        return false;
    }

    public List<ClientInfo> GetPlayersByRoomId(uint roomId)
    {
        var index = _roomPool.FindIndex(room => room.ID == roomId);

        if (index == -1 || !_roomPool[index].gameObject.activeInHierarchy)
            return new();

        return _roomPool[index].Clients;
    }

    public void JoinRoomFromServer(JoinRoom message)
    {
        var roomIndex = _roomPool.FindIndex(room => room.ID == message.roomId);

        if (roomIndex > -1)
            _roomPool[roomIndex].JoinRoom(message.client);
    }

    private uint GetNextID()
    {
        return ++_idGen;
    }

    public void LeaveRoomFromServer(uint userId, uint roomId)
    {
        // var room = _roomPool.
    }

    // -----------------------------------------------
    // -----------------------------------------------
    // ---------SERVER & CLIENT FUNCTIONS-------------
    // -----------------------------------------------
    // -----------------------------------------------
    public Room CreateRoom(ClientInfo roomMaster, uint roomId, int limitUser = 4)
    {
        Room room = GetNextRoom();

        if (room == null)
            return null;

        room.RoomInit(roomId, roomMaster, limitUser, _client.RequestJoinRoom);

        room.transform.SetAsLastSibling();

        room.gameObject.SetActive(true);

        return room;
    }

    public void JoinRoom(JoinRoom message)
    {
        var roomIndex = _roomPool.FindIndex(room => room.ID == message.roomId);

        if (roomIndex > -1)
        {
            _roomPool[roomIndex].JoinRoom(message.client);

            for (int i = 0; message.clientsInTheRoom != null && i < message.clientsInTheRoom.Length; i++)
            {
                if (message.clientsInTheRoom[i].id == _client.ID)
                    continue;

                _roomPool[roomIndex].JoinRoom(message.clientsInTheRoom[i]);
            }
        }
    }

    private Room GetNextRoom()
    {
        for (int i = 0; i < _roomPool.Count; i++)
        {
            if (!_roomPool[i].gameObject.activeInHierarchy)
                return _roomPool[i];
        }
        return null;
    }
}
