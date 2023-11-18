using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct RoomInfo
{
    public uint roomId;
    public int clientCount;
    public int limitUsers;
    public ClientInfo roomMaster;
    public RoomState state;
}

[Serializable]
public enum RoomState
{
    NotFull,
    Full,
    Playing,
}

public class Room : MonoBehaviour
{
    public void RoomInit(uint roomId, ClientInfo roomMaster, int limitUser)
    {
        _roomMaster = roomMaster;
        _roomId = roomId;
        _limitUsers = limitUser;
        _clients.Add(roomMaster);
    }
    //public User RoomMaster { get { return _roomMaster; } }
    //public List<User> Users { get { return _users; } }
    //public int LimitUsers { get { return _limitUsers; } }

    public RoomInfo GetRoomInfo()
    {
        RoomInfo roomInfo;

        roomInfo.roomId = _roomId;
        roomInfo.clientCount = _clients.Count;
        roomInfo.state = State;
        roomInfo.roomMaster = _roomMaster;
        roomInfo.limitUsers = _limitUsers;

        return roomInfo;
    }

    public uint ID { get => _roomId; }

    [SerializeField] private int _limitUsers = 4;
    [SerializeField] private uint _roomId = 0;
    [SerializeField] private RoomState _state = RoomState.NotFull;

    private List<ClientInfo> _clients = new();
    private ClientInfo _roomMaster;

    public RoomState State
    {
        get
        {
            return _state == RoomState.Playing ? _state : _limitUsers > _clients.Count ? RoomState.NotFull : RoomState.Full;
        }
    }
}
