using System.Collections.Generic;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    private List<ClientInfo> _clientsInTheGame = null;

    [SerializeField] private PlayerManager _playerManager = null;

    private UpdateGameWorld _gameWorldInfo;

    public void InitGame(List<ClientInfo> clients)
    {
        _clientsInTheGame = clients;
    }

    private void Start()
    {
        if (_playerManager != null)
            _playerManager.InitPlayerManager();
    }

    private void Update()
    {
        
    }

    public void SetPlayerPos(uint netid, Vector2 pos)
    {

    }
}
