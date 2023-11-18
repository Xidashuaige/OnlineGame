using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private int _maxRooms = 20;
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private GameObject roomParent;

    // Client use
    private readonly List<Room> _roomPool = new();

    // Server use
    private int _roomCreated = 0;
    private uint _idGen = 0;
    public bool CanCreateMore { get => _roomCreated >= _maxRooms ? false : true; }

    private void Start()
    {
        for (int i = 0; i < _maxRooms; i++)
        {
            var room = Instantiate(roomPrefab);
            room.SetActive(false);
            room.transform.SetParent(roomParent.transform);
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
    public uint CreateRoomFromServer(ClientInfo roomMaster, int maxUser = 4)
    {
        if (CanCreateMore)
        {
            _roomCreated++;

            var newRoom = CreateRoom(roomMaster, GetNextID(), maxUser);

            return newRoom.ID;
        }
        return 0;
    }

    private uint GetNextID()
    {
        return ++_idGen;
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

        room.RoomInit(roomId, roomMaster, limitUser);

        room.transform.SetAsLastSibling();

        room.gameObject.SetActive(true);

        return room;
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
