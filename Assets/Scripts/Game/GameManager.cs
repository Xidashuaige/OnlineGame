using System.Collections.Generic;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance = null;

    private List<ClientInfo> _clientsInTheGame = null;

    [SerializeField] private PlayerManager _playerManager = null;
    [SerializeField] private PanelManager _panelManager = null;

    private UpdateGameWorld _gameWorldInfo;

    private bool _inGame = false;

    public bool InGame { get => _inGame; }

    public void InitGame(List<ClientInfo> clients)
    {
        _clientsInTheGame = clients;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject.transform.parent);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (_playerManager != null)
            _playerManager.InitPlayerManager();

        Client.Instance.onActionHandlered[NetworkMessageType.StartGame] += OnGameStart;
        Client.Instance.onActionHandlered[NetworkMessageType.PlayerDead] += OnGameFinished;
    }

    public void GameOver()
    {
        if (_playerManager != null)
        {
            _playerManager.ReturnToWaitingRoom();
            _panelManager.ChangeScene(Panels.RoomPanel);
        }
    }

    private void OnGameStart(NetworkMessage data)
    {
        if (!data.succesful)
            return;

        _inGame = true;
    }

    private void OnGameFinished(NetworkMessage data)
    {
        if (!data.succesful)
            return;

        _inGame = false;
    }
}
