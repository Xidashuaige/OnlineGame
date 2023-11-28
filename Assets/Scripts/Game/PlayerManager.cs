using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;

    [SerializeField] private Dictionary<uint, PlayerController> _players = new();

    private void Start()
    {
        Client.Instante.onActionHandlered[NetworkMessageType.StartGame] += OnStartGame;
    }
    public void UpdatePlayerPosition(uint id, Vector2 newPos)
    {
        _players[id].transform.position = newPos;
    }

    public void AddPlayer(PlayerController player)
    {
        _players.Add(player.NetId, player);
    }

    public void OnStartGame(NetworkMessage data)
    {
        var message = data as StartGame;

        if (Client.Instante.RoomID != message.roomId)
            return;

        foreach (var item in message.playerIdsAndNetIds)
        {
            CreatePlayer(item.Value, item.Key == Client.Instante.ID);
        }
    }

    private void CreatePlayer(uint netId, bool owner)
    {
        GameObject player = Instantiate(_playerPrefab);

        player.transform.parent = transform;

        PlayerController playerController = player.GetComponent<PlayerController>();

        playerController.Owner = owner;

        playerController.NetId = netId;

        _players.Add(netId, playerController);
    }
}
