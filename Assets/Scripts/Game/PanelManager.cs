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

    [Space, Header("Socket Related")]
    [SerializeField] private Server _server;
    [SerializeField] private Client _client;

    private readonly List<GameObject> _panels = new();

    private GameObject _currentPanel;

    private void Start()
    {
        // server callbacks
        _server.onIpUpdate += OnIpUpdate;

        // client callbacks
        _client.onJoinServer += OnJoinServer;
        _client.onLeaveServer += OnLeaveServer;

        // Init Panels
        _panels.Add(_startPanel);
        _panels.Add(_roomListPanel);
        _panels.Add(_roomPanel);
        _panels.Add(_gamePanel);

        _currentPanel = _startPanel;
    }

    private void ChangeScene(Panels panel)
    {
        _currentPanel.SetActive(false);

        _currentPanel = _panels[(int)panel];

        _currentPanel.SetActive(true);
    }

    private void OnIpUpdate(string newIp)
    {
        _ipAdressesForCopy.text = newIp;

        _ipInput.text = newIp;
    }

    private void OnJoinServer()
    {
        ChangeScene(Panels.RoomListPanel);

        _nameTextInRoomList.text = _nameInput.text;

        if (_client.host) // If we're server host
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

    private void OnLeaveServer()
    {
        ChangeScene(Panels.StartPanel);
    }
}
