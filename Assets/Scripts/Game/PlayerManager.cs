using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;

    [SerializeField] private Dictionary<uint, PlayerController> _players = new();

    private void Start()
    {
        
    }
    public void UpdatePlayerPosition(uint id, Vector2 newPos)
    {
        _players[id].transform.position = newPos;
    }

    public void AddPlayer(PlayerController player)
    {
        _players.Add(player.NetId, player);
    }

    public void OnStartGame(StartGame message)
    {

    }

    public void CreatePlayer(uint id)
    {
        GameObject player =  Instantiate(_playerPrefab);

        PlayerController playerController = player.GetComponent<PlayerController>();

        playerController.NetId = id;

        _players.Add(id, playerController);
    }
}
