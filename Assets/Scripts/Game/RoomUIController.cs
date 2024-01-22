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

    public bool roomMaster = false;
    public TMP_Text name = null;
    public uint playerId = 0;
}


public class RoomUIController : MonoBehaviour
{
    [SerializeField] List<Sprite> _avatarSprites = new();
    [SerializeField] Sprite _noPlayerSprite;
    [SerializeField] GameObject _startBtn;
    [SerializeField] TMP_Text _roomIdLabel;

    private List<PlayerInTheRoomPanel> _players = new();

    public void InitRoomController()
    {
        // Get all images
        var images = GetComponentsInChildren<Image>();

        // Init avatar images
        var avatarImg = images.Where(avatar => avatar.gameObject.name == "Avatar").ToArray();

        foreach (var avatar in avatarImg)
        {
            avatar.sprite = _noPlayerSprite;

            PlayerInTheRoomPanel player = new();

            player.avatarImg = avatar;

            _players.Add(player);
        }

        // Init room master images
        var roomMasterImg = images.Where(roomMaster => roomMaster.gameObject.name == "RM").ToArray();

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

        Client.Instance.onActionHandlered[NetworkMessageType.JoinRoom] += JoinRoom;
        Client.Instance.onActionHandlered[NetworkMessageType.LeaveRoom] += OnLeaveTheRoom;
    }

    private void JoinRoom(NetworkMessage data)
    {
        var message = data as JoinRoom;

        if (Client.Instance.RoomID != message.roomId)
            return;

        // Add current player
        var player = _players[_players.FindIndex(p => p.playerId == 0)];

        player.playerId = message.client.id;
        player.avatarImg.sprite = _avatarSprites[Random.Range(0, _avatarSprites.Count)];
        player.name.text = message.client.name;
        player.roomMaster = message.client.isRoomMaster;
        if (player.roomMaster)
            player.roomMasterImg.gameObject.SetActive(true);

        _roomIdLabel.text = "Room ID: " + message.roomId;

        if (Client.Instance.RoomMaster)
            _startBtn.SetActive(true);

        // Add other players
        for (int i = 0; message.messageOwnerId == Client.Instance.ID && message.clientsInTheRoom != null && i < message.clientsInTheRoom.Count; i++)
        {
            if (message.clientsInTheRoom[i].id == Client.Instance.ID)
                continue;

            // Debug.Log("(" + _client.Name + ") Add client" + i + " in the room");

            player = _players[_players.FindIndex(p => p.playerId == 0)];
            player.playerId = message.clientsInTheRoom[i].id;
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

    private void OnLeaveTheRoom(NetworkMessage data)
    {
        var message = data as LeaveRoom;

        if (Client.Instance.RoomID != message.roomId)
            return;

        // If leaver is room master or me, then, close the room (UI)
        if (message.isRoomMaster || message.messageOwnerId == Client.Instance.ID)
        {
            CloseRoom();
            return;
        }

        // If not, remove the correspond player in the room
        var player = _players[_players.FindIndex(player => player.playerId == message.messageOwnerId)];

        player.playerId = 0;
        player.avatarImg.sprite = _noPlayerSprite;
        player.name.text = "";
        player.roomMaster = false;
        player.roomMasterImg.gameObject.SetActive(false);
    }

    private void CloseRoom()
    {
        Client.Instance.RoomMaster = false;

        foreach (var player in _players)
        {
            player.playerId = 0;
            player.avatarImg.sprite = _noPlayerSprite;
            player.name.text = "";
            player.roomMasterImg.gameObject.SetActive(false);
        }

        _startBtn.SetActive(false);
    }
}
