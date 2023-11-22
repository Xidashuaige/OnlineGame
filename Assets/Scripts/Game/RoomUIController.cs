using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

class PlayerInTheRoomPanel
{
    public Image avatarImg = null;
    public Image roomMasterImg = null;
    public Button button = null;
    public TMP_Text ready = null;
    public bool inTheRoom = false;

    public bool roomMaster = false;
    public TMP_Text name = null;
    public uint playerId = 0;
}


public class RoomUIController : MonoBehaviour
{
    [SerializeField] List<Sprite> _avatarSprites = new();
    [SerializeField] Sprite _noPlayerSprite;
    [SerializeField] Client _client;

    private List<PlayerInTheRoomPanel> _players = new();

    public void InitRoomController()
    {
        // Get all images
        var images = GetComponentsInChildren<Image>();

        foreach (var image in images)
        {
            Debug.Log(image.name);
        }

        // Init avatar images
        var avatarImg = images.Where(avatar => avatar.gameObject.name == "Avatar").ToArray();
        Debug.Log("avatarImg:" + avatarImg.Length);

        foreach (var avatar in avatarImg)
        {
            avatar.sprite = _noPlayerSprite;

            PlayerInTheRoomPanel player = new();

            player.avatarImg = avatar;

            _players.Add(player);
        }

        // Init room master images
        var roomMasterImg = images.Where(roomMaster => roomMaster.gameObject.name == "RM").ToArray();

        Debug.Log("roomMasterImg:" + roomMasterImg.Length);

        for (int i = 0; i < roomMasterImg.Length; i++)
        {
            roomMasterImg[i].gameObject.SetActive(false);

            _players[i].roomMasterImg = roomMasterImg[i];
        }

        // Get all text
        var texts = GetComponentsInChildren<TMP_Text>();

        // Init player name
        var nameText = texts.Where(text => text.gameObject.name == "Player name").ToArray();
        for (int i = 0; i < nameText.Length; i++)
        {
            nameText[i].text = "";

            _players[i].name = nameText[i];
        }

        // Init player ready
        var readyText = texts.Where(text => text.gameObject.name == "ready").ToArray();
        for (int i = 0; i < readyText.Length; i++)
        {
            readyText[i].text = "";

            _players[i].ready = readyText[i];
        }

        // Init player button
        var buttons = GetComponentsInChildren<Button>();

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = false;
            _players[i].button = buttons[i];
        }

        if (_client == null)
            _client = GameObject.FindWithTag("Client").GetComponent<Client>();

        _client.onJoinRoom += JoinRoom;
        _client.onLeaveRoom += CloseRoom;
    }

    private void JoinRoom(JoinRoom message)
    {
        // Add current player
        var player = _players[_players.FindIndex(p => p.inTheRoom == false)];
        player.inTheRoom = true;
        player.avatarImg.sprite = _avatarSprites[Random.Range(0, _avatarSprites.Count)];
        player.name.text = message.client.name;
        player.roomMaster = message.client.isRoomMaster;
        if (player.roomMaster)
            player.roomMasterImg.gameObject.SetActive(true);

        // Add other players
        for (int i = 0; message.messageOwnerId == _client.ID && message.clientsInTheRoom != null && i < message.clientsInTheRoom.Count; i++)
        {
            if (message.clientsInTheRoom[i].id == _client.ID)
                continue;

            // Debug.Log("(" + _client.Name + ") Add client" + i + " in the room");

            player = _players[_players.FindIndex(p => p.inTheRoom == false)];
            player.inTheRoom = true;
            player.avatarImg.sprite = _avatarSprites[Random.Range(0, _avatarSprites.Count)];
            player.name.text = message.clientsInTheRoom[i].name;
            player.roomMaster = message.clientsInTheRoom[i].isRoomMaster;

            if (player.roomMaster)
                player.roomMasterImg.gameObject.SetActive(true);
        }
    }

    private void UpdateRoomController()
    {

    }

    private void LeaveTheRoom(uint playerId)
    {
        var player = _players[_players.FindIndex(player => player.inTheRoom == false)];

        player.inTheRoom = false;
        player.avatarImg.sprite = _noPlayerSprite;
        player.name.text = "";
        player.roomMasterImg.gameObject.SetActive(false);
    }

    private void CloseRoom()
    {
        foreach (var player in _players)
        {
            player.inTheRoom = false;
            player.avatarImg.sprite = _noPlayerSprite;
            player.name.text = "";
            player.roomMasterImg.gameObject.SetActive(false);
        }
    }
}
