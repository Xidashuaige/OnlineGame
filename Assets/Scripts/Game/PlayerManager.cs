using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;

    [SerializeField] private Dictionary<uint, PlayerController> _players = new();

    private bool _gameStarted = false;

    public void InitPlayerManager()
    {
        Client.Instante.onActionHandlered[NetworkMessageType.StartGame] += OnStartGame;
        Client.Instante.onActionHandlered[NetworkMessageType.UpdatePlayerPosition] += OnUpdatePlayerPosition;
    }

    private void CreatePlayer(uint netId, bool owner)
    {
        // Create player and init his position
        GameObject player = Instantiate(_playerPrefab, transform);

        player.transform.position += (Vector3)(Vector2.left * Random.value);

        // Init player controller
        PlayerController playerController = player.GetComponent<PlayerController>();

        playerController.Owner = owner;

        playerController.NetId = netId;

        playerController.InitPlayerController();

        // Add playerController to the list
        _players.Add(netId, playerController);
    }

    public void AddPlayer(PlayerController player)
    {
        _players.Add(player.NetId, player);
    }

    private void OnStartGame(NetworkMessage data)
    {
        var message = data as StartGame;

        if (Client.Instante.RoomID != message.roomId)
            return;

        for (int i = 0; i < message.playerIds.Count; i++)
        {
            CreatePlayer(message.netIds[i], message.playerIds[i] == Client.Instante.ID);
        }

        _gameStarted = true;
    }

    private void OnUpdatePlayerPosition(NetworkMessage data)
    {
        if (!_gameStarted)
            return;

        var message = data as UpdatePlayerMovement;

        _players[message.netId].transform.position = message.position;
    }
}
