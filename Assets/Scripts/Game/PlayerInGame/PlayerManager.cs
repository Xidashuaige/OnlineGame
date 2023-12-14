using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _birdPrefab;

    [SerializeField] private Dictionary<uint, PlayerController> _players = new();
    [SerializeField] private Dictionary<uint, BirdController> _birds = new();

    private bool _gameStarted = false;

    public void InitPlayerManager()
    {
        Client.Instante.onActionHandlered[NetworkMessageType.StartGame] += OnStartGame;
        Client.Instante.onActionHandlered[NetworkMessageType.UpdatePlayerPosition] += OnUpdatePlayerPosition;
        Client.Instante.onActionHandlered[NetworkMessageType.UpdateBirdPosition] += OnUpdateBirdPosition;  
    }

    private void CreatePlayer(uint netId, bool owner, string name)
    {
        // Create player and init his position
        var player = Instantiate(_playerPrefab, transform);

        player.transform.position += (Vector3)(Vector2.left * Random.value);

        // Init player controller
        PlayerController playerController = player.GetComponent<PlayerController>();

        playerController.InitPlayerController(netId, owner, name);

        // Create bird and init his position
        var bird = Instantiate(_birdPrefab, transform);

        bird.transform.position += (Vector3)(Vector2.left * Random.value);

        // Init player controller
        BirdController birdController = bird.GetComponent<BirdController>();

        //birdController.InitBirdController(netId2, owner)
        //there will have another netid


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
            CreatePlayer(message.playerNetIds[i], message.playerIds[i] == Client.Instante.ID, message.names[i]);
        }

        _gameStarted = true;
    }

    private void OnUpdatePlayerPosition(NetworkMessage data)
    {
        if (!_gameStarted)
            return;

        var message = data as UpdatePlayerMovement;

        _players[message.netId].SetPosition(message.position, message.flipX, message.timeUsed);
    }

    private void OnUpdateBirdPosition(NetworkMessage data)
    {
        if (!_gameStarted)
            return;

        var message = data as UpdatePlayerMovement;

        _birds[message.netId].SetPosition(message.position, message.flipX, message.timeUsed);
    }
}
