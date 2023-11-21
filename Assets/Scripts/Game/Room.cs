using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RoomInfo // For Server
{
    public RoomInfo(uint roomId, ClientInfo roomMaster, int limitUsers = 4, RoomState state = RoomState.NotFull)
    {
        this.roomId = roomId;
        clients = new ClientInfo[limitUsers];
        clients[0] = roomMaster;
        this.state = state;
    }

    public uint roomId;
    public int limitUsers;
    public ClientInfo[] clients;
    public RoomState state;
    public bool IsFull { get => limitUsers <= clients.Length; }
}

[Serializable]
public enum RoomState
{
    NotFull,
    Full,
    Playing,
}

public class Room : MonoBehaviour // For Client
{
    // Room info
    [SerializeField] private int _limitUsers = 4;
    [SerializeField] private uint _roomId = 0;
    [SerializeField] private RoomState _state = RoomState.NotFull;
    private ClientInfo _roomMaster;
    private List<ClientInfo> _clients = new();
    public List<ClientInfo> Clients { get => _clients; }
    public bool IsFull { get => _limitUsers <= _clients.Count; }

    public uint ID { get => _roomId; }

    // Unity objects
    private Button _btn = null;

    // Event
    private Action<uint> _onJoinRoomRequest;

    public void RoomInit(uint roomId, ClientInfo roomMaster, int limitUser, Action<uint> onJoinRoomAction = null)
    {
        // Init room init
        _roomMaster = roomMaster;
        _roomId = roomId;
        _limitUsers = limitUser;
        _clients.Add(roomMaster);
        _state = RoomState.NotFull;

        // Unity Setting
        transform.SetAsLastSibling();

        gameObject.SetActive(true);

        if (_btn == null)
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(OnbtnCLick);
            _onJoinRoomRequest += onJoinRoomAction;
        }
    }

    public void JoinRoom(ClientInfo client)
    {
        _clients.Add(client);
    }

    public void LeaveRoom(ClientInfo client)
    {
        _clients.Remove(client);
    }

    public void CloseRoom()
    {
        _clients.Clear();
        _roomMaster = null;
        _roomId = 0;
        _limitUsers = 4;
        _state = RoomState.NotFull;
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
