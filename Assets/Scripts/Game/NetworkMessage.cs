using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEngine;

public enum NetworkMessageType
{
    Null,
    JoinServer,
    LeaveServer,
    JoinRoom,
    LeaveRoom,
    CreateRoom,
    ReadyInTheRoom,
    StartGame,
    KickOutRoom,
    Heartbeat,
    CloseServer,

    // Game actions
    UpdatePlayerPosition,
    UpdateBirdPosition,
    UpdateGameWorld,

    MaxCount,
}

[Serializable]
public class NetworkPackage
{
    public NetworkPackage(NetworkMessageType type, byte[] data)
    {
        this.type = type;
        this.data = data;
    }

    public byte[] GetBytes()
    {
        string test = JsonUtility.ToJson(this);

        NetworkPackage networkPackage = JsonUtility.FromJson<NetworkPackage>(test);

        return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    public NetworkMessage GetData()
    {
        return _getDataActions[type].Invoke(data);
    }

    public static NetworkMessage GetDataFromBytes(byte[] data, int lenght)
    {
        var jsonString = Encoding.ASCII.GetString(data, 0, lenght);

        var networkPackage = JsonUtility.FromJson<NetworkPackage>(jsonString);

        return networkPackage.GetData();
    }

    public NetworkMessageType type;

    public byte[] data;

    private delegate NetworkMessage GetDataAction(byte[] data);

    static private Dictionary<NetworkMessageType, GetDataAction> _getDataActions = new()
    {
        { NetworkMessageType.Heartbeat, HearthBeat.GetData },
        { NetworkMessageType.JoinServer, JoinServer.GetData },
        { NetworkMessageType.LeaveServer, LeaveServer.GetData },
        { NetworkMessageType.CreateRoom, CreateRoom.GetData },
        { NetworkMessageType.JoinRoom, JoinRoom.GetData },
        { NetworkMessageType.LeaveRoom, LeaveRoom.GetData },
        { NetworkMessageType.ReadyInTheRoom, ReadyInTheRoom.GetData },
        { NetworkMessageType.StartGame, StartGame.GetData },
        { NetworkMessageType.KickOutRoom, KickOutRoom.GetData },

        { NetworkMessageType.UpdatePlayerPosition, UpdatePlayerMovement.GetData },
        { NetworkMessageType.UpdateBirdPosition, UpdateBirdMovement.GetData },
        { NetworkMessageType.UpdateGameWorld, UpdateGameWorld.GetData},
    };
}

[Serializable]
public class NetworkMessage
{
    protected NetworkMessage(NetworkMessageType type, uint messageOwnerId)
    {
        this.type = type;
        this.messageOwnerId = messageOwnerId;
    }

    virtual public byte[] GetBytes()
    {
        return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    // 4 server & client
    public NetworkMessageType type = NetworkMessageType.Null;

    public uint messageOwnerId = 0;

    // 4 client
    public bool succesful = false;

    public EndPoint endPoint = null;

    public EndPoint testEndPoint = null;
}

[Serializable]
public class HearthBeat : NetworkMessage
{
    public HearthBeat(uint userId) : base(NetworkMessageType.Heartbeat, userId) { }

    public override byte[] GetBytes()
    {
        return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    static public HearthBeat GetData(byte[] data)
    {
        return JsonUtility.FromJson<HearthBeat>(Encoding.ASCII.GetString(data, 0, data.Length));
    }
}

// -----------------------------------------------
// ------------REQUEST IN THE SERVER--------------
// -----------------------------------------------

[Serializable]
public class JoinServer : NetworkMessage
{
    public JoinServer(string userName) : base(NetworkMessageType.JoinServer, 0)
    {
        name = userName;
    }

    static public JoinServer GetData(byte[] data)
    {
        return JsonUtility.FromJson<JoinServer>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    public void AddRooms(List<RoomInfo> rooms)
    {
        roomsId = new();
        currentPlayers = new();
        maxPlayers = new();
        roomState = new();

        foreach (RoomInfo room in rooms)
        {
            roomsId.Add(room.id);
            currentPlayers.Add(room.clients == null ? 0 : room.clients.Count);
            maxPlayers.Add(room.limitUsers);
            roomState.Add(room.state);
        }
    }

    // 4 server
    public string name;

    // 4 clients
    public List<uint> roomsId = null;
    public List<int> currentPlayers = null;
    public List<int> maxPlayers = null;
    public List<RoomState> roomState = null;
}

[Serializable]
public class LeaveServer : NetworkMessage
{
    public LeaveServer(uint userId, bool forceLeave = false) : base(NetworkMessageType.LeaveServer, userId)
    {
        succesful = forceLeave;
    }

    static public LeaveServer GetData(byte[] data)
    {
        return JsonUtility.FromJson<LeaveServer>(Encoding.ASCII.GetString(data, 0, data.Length));
    }
}

[Serializable]
public class CreateRoom : NetworkMessage
{
    public CreateRoom(uint userId, int maxUser = 4) : base(NetworkMessageType.CreateRoom, userId)
    {
        this.maxUser = maxUser;
    }

    static public CreateRoom GetData(byte[] data)
    {
        return JsonUtility.FromJson<CreateRoom>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    // 4 server
    public int maxUser;

    // 4 client
    public uint roomId;
}

[Serializable]
public class UpdateRoomListInfo : NetworkMessage
{
    public UpdateRoomListInfo(uint userId) : base(NetworkMessageType.CreateRoom, userId) { }

    static public UpdateRoomListInfo GetData(byte[] data)
    {
        return JsonUtility.FromJson<UpdateRoomListInfo>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    // 4 client
    public List<RoomInfo> _roomsInfo;
}

[Serializable]
public class JoinRoom : NetworkMessage
{
    public JoinRoom(uint userId, uint roomId, string playerName = "Default", bool roomMaster = false) : base(NetworkMessageType.JoinRoom, userId)
    {
        client = new(playerName, userId, null, roomMaster);
        this.roomId = roomId;
    }

    static public JoinRoom GetData(byte[] data)
    {
        return JsonUtility.FromJson<JoinRoom>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    // 4 server
    public uint roomId;

    // 4 client
    public uint roomMasterId;
    public List<ClientInfo> clientsInTheRoom;
    public ClientInfo client;
}

// -----------------------------------------------
// -----------------------------------------------
// ------------REQUEST IN THE ROOM----------------
// -----------------------------------------------
// -----------------------------------------------

[Serializable]
public class LeaveRoom : NetworkMessage
{
    public LeaveRoom(uint userId, uint roomId) : base(NetworkMessageType.LeaveRoom, userId)
    {
        this.roomId = roomId;
    }

    static public LeaveRoom GetData(byte[] data)
    {
        return JsonUtility.FromJson<LeaveRoom>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    // 4 server
    public uint roomId;

    // 4 clients
    public bool isRoomMaster;
}

[Serializable]
public class ReadyInTheRoom : NetworkMessage
{
    public ReadyInTheRoom(uint userId) : base(NetworkMessageType.ReadyInTheRoom, userId) { }

    static public ReadyInTheRoom GetData(byte[] data)
    {
        return JsonUtility.FromJson<ReadyInTheRoom>(Encoding.ASCII.GetString(data, 0, data.Length));
    }
}

// Just for Room Master
[Serializable]
public class StartGame : NetworkMessage
{
    public StartGame(uint userId, uint roomId) : base(NetworkMessageType.StartGame, userId)
    {
        this.roomId = roomId;
    }

    static public StartGame GetData(byte[] data)
    {
        return JsonUtility.FromJson<StartGame>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    // 3 server & client
    public uint roomId = 0;

    // 4 client]
    public List<uint> playerIds;
    public List<uint> playerNetIds;
    public List<uint> birdNetIds;
    public List<string> names;
}

// Just for Room Master
[Serializable]
public class KickOutRoom : NetworkMessage
{
    public KickOutRoom(uint userId, uint targetUserId) : base(NetworkMessageType.KickOutRoom, userId)
    {
        this.targetUserId = targetUserId;
    }

    static public KickOutRoom GetData(byte[] data)
    {
        return JsonUtility.FromJson<KickOutRoom>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    // 4 server & client
    public uint targetUserId;
}

// -----------------------------------------------
// -----------------------------------------------
// ------------REQUEST IN THE GAME----------------
// -----------------------------------------------
// -----------------------------------------------

[SerializeField]
public class UpdatePlayerMovement : NetworkMessage
{
    public UpdatePlayerMovement(uint userId, uint netId, Vector2 position, bool flipX, float timeUsed) : base(NetworkMessageType.UpdatePlayerPosition, userId)
    {
        this.netId = netId;
        this.position = position;
        this.flipX = flipX;
        this.timeUsed = timeUsed;
    }

    static public UpdatePlayerMovement GetData(byte[] data)
    {
        return JsonUtility.FromJson<UpdatePlayerMovement>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    // 4 server & clients
    public bool flipX;
    public float timeUsed;
    public uint netId;
    public Vector2 position;
}

[SerializeField]
public class UpdateBirdMovement : NetworkMessage
{
    public UpdateBirdMovement(uint userId, uint netId, Vector2 position, bool flipX, float timeUsed) : base(NetworkMessageType.UpdateBirdPosition, userId)
    {
        this.netId = netId;
        this.position = position;
        this.flipX = flipX;
        this.timeUsed = timeUsed;
    }

    static public UpdateBirdMovement GetData(byte[] data)
    {
        return JsonUtility.FromJson<UpdateBirdMovement>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    // 4 server & clients
    public bool flipX;
    public float timeUsed;
    public uint netId;
    public Vector2 position;
}

[SerializeField]
public class UpdateGameWorld : NetworkMessage
{
    UpdateGameWorld(uint userId) : base(NetworkMessageType.Null, userId)
    {

    }

    static public UpdateGameWorld GetData(byte[] data)
    {
        return JsonUtility.FromJson<UpdateGameWorld>(Encoding.ASCII.GetString(data, 0, data.Length));
    }
}