using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEngine;

public enum NetworkMessageType
{
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
    Null
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

    #region Factory Pattern

    public static NetworkPackage CreateJoinServerRequest(uint userId)
    {
        HearthBeat message = new(userId);

        return new(NetworkMessageType.Heartbeat, message.GetBytes());
    }

    public static NetworkPackage CreateJoinServerRequest(string userName)
    {
        JoinServer message = new(userName);

        return new(NetworkMessageType.JoinServer, message.GetBytes());
    }

    public static NetworkPackage CreateLeaveServerRequest(uint userId)
    {
        LeaveServer message = new(userId);

        return new(NetworkMessageType.LeaveServer, message.GetBytes());
    }

    public static NetworkPackage CreateCreateRoomRequest(uint userId, int maxPlayer = 4)
    {
        CreateRoom message = new(userId, maxPlayer);

        return new(NetworkMessageType.CreateRoom, message.GetBytes());
    }

    public static NetworkPackage CreateJoinRoomRequest(uint userId, uint roomId, string name = "Default", bool isRoomMaster = false)
    {
        JoinRoom message = new(userId, roomId, name, isRoomMaster);

        return new(NetworkMessageType.JoinRoom, message.GetBytes());
    }

    public static NetworkPackage CreateLeaveRoomRequest(uint userId, uint roomId)
    {
        LeaveRoom message = new(userId, roomId);

        return new(NetworkMessageType.LeaveRoom, message.GetBytes());
    }

    public static NetworkPackage CreateReadyInTheRoomRequest(uint userId)
    {
        ReadyInTheRoom message = new(userId);

        return new(NetworkMessageType.ReadyInTheRoom, message.GetBytes());
    }

    public static NetworkPackage CreateStartGameRequest(uint userId)
    {
        StartGame message = new(userId);

        return new(NetworkMessageType.StartGame, message.GetBytes());
    }

    public static NetworkPackage CreateKickOutRoomRequest(uint userId, uint targetId)
    {
        KickOutRoom message = new(userId, targetId);

        return new(NetworkMessageType.KickOutRoom, message.GetBytes());
    }

    #endregion

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

    // 4 server
    public string name;
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
    public CreateRoom(uint userId, int maxUser) : base(NetworkMessageType.CreateRoom, userId)
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
    public ClientInfo roomMaster;
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
    public ClientInfo client;

    // 4 client
    public uint roomMasterId;
    public List<ClientInfo> clientsInTheRoom;
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
    public StartGame(uint userId) : base(NetworkMessageType.StartGame, userId) { }

    static public StartGame GetData(byte[] data)
    {
        return JsonUtility.FromJson<StartGame>(Encoding.ASCII.GetString(data, 0, data.Length));
    }
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