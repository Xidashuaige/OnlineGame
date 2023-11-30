using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    private List<ClientInfo> _clientsInTheGame = null;

    [SerializeField] private PlayerManager _playerManager = null;

    public void InitGame(List<ClientInfo> clients)
    {
        _clientsInTheGame = clients;
    }

    private void Start()
    {
        if (_playerManager != null)
            _playerManager.InitPlayerManager();
    }
}
