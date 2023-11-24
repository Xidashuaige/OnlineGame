using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RoomInfo // For Server
{
    public RoomInfo(uint roomId, ClientInfo roomMaster, int limitUsers = 4, RoomState state = RoomState.NotFull)
    {
        id = roomId;
        clients = new();
        if (roomMaster != null)
            clients.Add(roomMaster);

        this.state = state;
        this.limitUsers = limitUsers;
    }

    ~RoomInfo()
    {
        clients.Clear();
        clients = null;
    }

    public uint id = 0;
    public int limitUsers = 4;
    // BUG, only run is ClientInfo is array
    public List<ClientInfo> clients = null;
    public RoomState state = RoomState.NotFull;
    public bool IsFull { get => clients != null && limitUsers <= clients.Count; }
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
    [SerializeField] private TMP_Text _roomName;
    [SerializeField] private TMP_Text _roomPlayers;
    [SerializeField] private Image _stateImage;

    private int _playerCount = 0;
    public bool IsFull { get => _limitUsers <= _playerCount; }

    public uint ID { get => _roomId; }

    // Unity objects
    private Button _btn = null;

    // Event
    private Action<uint> _onJoinRoomRequest;

    public void RoomInit(uint roomId, int limitUser, Action<uint> onJoinRoomAction = null, int playerCount = 0, RoomState state = RoomState.NotFull)
    {
        // Init room init
        _roomId = roomId;
        _limitUsers = limitUser;
        _state = state;
        _playerCount = playerCount;

        _roomName.text = "Room " + roomId.ToString("D4");
        _roomPlayers.text = _playerCount + "/" + _limitUsers;

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

    public bool JoinRoom()
    {
        if (IsFull)
            return false;

        _playerCount++;

        _roomPlayers.text = _playerCount + "/" + _limitUsers;

        return true;
    }

    public void LeaveRoom()
    {
        _playerCount--;

        _roomPlayers.text = _playerCount + "/" + _limitUsers;
    }

    public void CloseRoom()
    {
        _playerCount = 0;
        _roomId = 0;
        _limitUsers = 4;
        _state = RoomState.NotFull;
        gameObject.SetActive(true);
    }

    public RoomState State { get => _state; }

    private void OnbtnCLick()
    {
        _onJoinRoomRequest?.Invoke(ID);
    }
}
