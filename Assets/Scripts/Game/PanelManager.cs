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

    [Space, Header("Unity objects")]
    [SerializeField] private TMP_Text[] _ipAdresses;
    [SerializeField] private TMP_InputField _ipInput;

    [Space, Header("Socket Related")]
    [SerializeField] private Server _server;

    private readonly List<GameObject> _panels = new();

    private GameObject _currentPanel;

    private void Start()
    {
        _currentPanel = _startPanel;

        _server.onIpUpdate += OnIpUpdate;

        _panels.Add(_startPanel);
        _panels.Add(_roomListPanel);
        _panels.Add(_roomPanel);
        _panels.Add(_gamePanel);
    }

    public void ChangeScene(Panels panel)
    {
        _currentPanel.SetActive(false);

        _currentPanel = _panels[(int)panel];

        _currentPanel.SetActive(true);
    }

    private void OnIpUpdate(string newIp)
    {
        foreach (var adr in _ipAdresses)
            adr.text = newIp;

        _ipInput.text = newIp;
    }
}
