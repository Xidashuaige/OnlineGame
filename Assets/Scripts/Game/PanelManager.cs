using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum Panels
{
    StartPanel,
    RoomListPanel,
    RoomPanel,
    GamePanel
}

public class PanelManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _startPanel;
    [SerializeField] private GameObject _roomListPanel;
    [SerializeField] private GameObject _roomPanel;
    [SerializeField] private GameObject _gamePanel;

    [Space, Header("Start Panel UI")]
    [SerializeField] private TMP_InputField _ipInput;
    [SerializeField] private TMP_InputField _nameInput;

    [Space, Header("Room List Panel UI")]
    [SerializeField] private TMP_Text _ipAdressesForCopy;
    [SerializeField] private TMP_Text _nameTextInRoomList;
    [SerializeField] private TMP_Text _avatarText;
    [SerializeField] private GameObject _btnCopyIp;

    private readonly List<GameObject> _panels = new();

    private Panels _currentPanel;

    private void Start()
    {
        // server callbacks
        Server.Instance.onIpUpdate += OnIpUpdate;

        // client callbacks
        Client.Instante.onActionHandlered[NetworkMessageType.JoinServer] += OnJoinServer;
        Client.Instante.onActionHandlered[NetworkMessageType.LeaveServer] += OnLeaveServer;
        Client.Instante.onActionHandlered[NetworkMessageType.JoinRoom] += OnJoinRoom;
        Client.Instante.onActionHandlered[NetworkMessageType.LeaveRoom] += OnLeaveRoom;
        Client.Instante.onActionHandlered[NetworkMessageType.StartGame] += OnStartGame;

        // Init Panels
        _panels.Add(_startPanel);
        _panels.Add(_roomListPanel);
        _panels.Add(_roomPanel);
        _panels.Add(_gamePanel);

        _currentPanel = Panels.StartPanel;
    }

    private void ChangeScene(Panels panel)
    {
        _panels[(int)_currentPanel].SetActive(false);

        _currentPanel = panel;

        _panels[(int)_currentPanel].SetActive(true);
    }

    private void OnIpUpdate(string newIp)
    {
        _ipAdressesForCopy.text = newIp;

        _ipInput.text = newIp;
    }

    private void OnJoinServer(NetworkMessage message)
    {
        ChangeScene(Panels.RoomListPanel);

        _nameTextInRoomList.text = _nameInput.text;

        if (Client.Instante.host) // If we're server host
        {
            _btnCopyIp.SetActive(true);
            _avatarText.text = "S";
        }
        else // If not
        {
            _btnCopyIp.SetActive(false);
            _avatarText.text = "C";
        }
    }
    private void OnLeaveServer(NetworkMessage message)
    {
        ChangeScene(Panels.StartPanel);
    }
    private void OnJoinRoom(NetworkMessage data)
    {
        var message = data as JoinRoom;

        if (Client.Instante.RoomID != message.roomId || _currentPanel == Panels.RoomPanel)
            return;

        ChangeScene(Panels.RoomPanel);
    }

    private void OnLeaveRoom(NetworkMessage data)
    {
        var message = data as LeaveRoom;

        if (Client.Instante.ID == message.messageOwnerId || message.isRoomMaster)
            ChangeScene(Panels.RoomListPanel);
    }

    private void OnStartGame(NetworkMessage message)
    {
        if (_currentPanel != Panels.GamePanel)
            ChangeScene(Panels.GamePanel);
    }
}
