using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private int _limitUsers = 4;
    [SerializeField] private uint _roomId = 0;
    [SerializeField] private RoomState _state = RoomState.NotFull;

    public bool IsFull { get => _limitUsers <= _clients.Count; }

    private List<ClientInfo> _clients = new();
    private ClientInfo _roomMaster;
    private Button _btn = null;
    private Action<uint> _onJoinRoomRequest;

    public uint ID { get => _roomId; }

    public void RoomInit(uint roomId, ClientInfo roomMaster, int limitUser, Action<uint> onJoinRoomAction = null)
    {
        if (_btn == null)
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(OnbtnCLick);
            _onJoinRoomRequest += onJoinRoomAction;
        }

        _roomMaster = roomMaster;
        _roomId = roomId;
        _limitUsers = limitUser;
        _clients.Add(roomMaster);
    }

    public void JoinRoom(ClientInfo client)
    {
        _clients.Add(client);
    }

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

    public RoomState State
    {
        get
        {
            return _state == RoomState.Playing ? _state : _limitUsers > _clients.Count ? RoomState.NotFull : RoomState.Full;
        }
    }

    private void OnbtnCLick()
    {
        _onJoinRoomRequest?.Invoke(ID);
    }
}
