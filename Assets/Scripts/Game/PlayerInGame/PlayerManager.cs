using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _birdPrefab;

    [SerializeField] private Dictionary<uint, PlayerController> _players = new();
    [SerializeField] private Dictionary<uint, BirdController> _birds = new();

    public void InitPlayerManager()
    {
        Client.Instance.onActionHandlered[NetworkMessageType.StartGame] += OnStartGame;
        Client.Instance.onActionHandlered[NetworkMessageType.UpdatePlayerPosition] += OnUpdatePlayerPosition;
        Client.Instance.onActionHandlered[NetworkMessageType.UpdateBirdPosition] += OnUpdateBirdPosition;
    }

    public void ReturnToWaitingRoom()
    {
        foreach (var item in _players)
        {
            Destroy(item.Value.gameObject);
        }

        _players.Clear();

        foreach (var item in _birds)
        {
            Destroy(item.Value.gameObject);
        }

        _birds.Clear();
    }

    private void CreatePlayer(uint netId, bool owner, string name)
    {
        // Create player and init his position
        var player = Instantiate(_playerPrefab, transform);

        player.transform.position += (Vector3)(Vector2.left * Random.value);

        // Init player controller
        PlayerController playerController = player.GetComponent<PlayerController>();

        playerController.InitPlayerController(netId, owner, name);

        // Add playerController to the list
        _players.Add(netId, playerController);
    }

    private void CreateBird(uint netId, bool owner)
    {
        // Create bird and init his position
        var bird = Instantiate(_birdPrefab, transform);

        bird.transform.position += (Vector3)(Vector2.left * Random.value);

        // Init player controller
        BirdController birdController = bird.GetComponent<BirdController>();

        birdController.InitBirdController(netId, owner);
        //there will have another netid

        _birds.Add(netId, birdController);
    }

    public void AddPlayer(PlayerController player)
    {
        _players.Add(player.NetId, player);
    }

    private void OnStartGame(NetworkMessage data)
    {
        var message = data as StartGame;

        if (Client.Instance.RoomID != message.roomId)
            return;

        ReturnToWaitingRoom();

        for (int i = 0; i < message.playerIds.Count; i++)
        {
            CreatePlayer(message.playerNetIds[i], message.playerIds[i] == Client.Instance.ID, message.names[i]);
        }

        for (int i = 0; i < message.birdNetIds.Count; i++)
        {
            CreateBird(message.birdNetIds[i], message.playerIds[i] == Client.Instance.ID);
        }
    }

    private void OnUpdatePlayerPosition(NetworkMessage data)
    {
        if (!GameManager.Instance.InGame)
            return;

        var message = data as UpdatePlayerMovement;

        _players[message.netId].SetPosition(message.position, message.flipX, message.timeUsed);
    }

    private void OnUpdateBirdPosition(NetworkMessage data)
    {
        if (!GameManager.Instance.InGame)
            return;

        var message = data as UpdateBirdMovement;

        _birds[message.netId].SetPosition(message.position, message.flipX, message.timeUsed);
    }
}
