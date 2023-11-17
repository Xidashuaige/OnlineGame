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

    public static NetworkPackage CreateJoinServerRequest(uint _userId)
    {
        HearthBeat message = new(_userId);

        return new(NetworkMessageType.Heartbeat, message.GetBytes());
    }

    public static NetworkPackage CreateJoinServerRequest(string _userName)
    {
        JoinServer message = new(_userName);

        return new(NetworkMessageType.JoinServer, message.GetBytes());
    }

    public static NetworkPackage CreateLeaveServerRequest(uint _userId)
    {
        LeaveServer message = new(_userId);

        return new(NetworkMessageType.LeaveServer, message.GetBytes());
    }

    public static NetworkPackage CreateCreateRoomRequest(uint _userId)
    {
        CreateRoom message = new(_userId);

        return new(NetworkMessageType.CreateRoom, message.GetBytes());
    }

    public static NetworkPackage CreateJoinRoomRequest(uint _userId, uint _roomId)
    {
        JoinRoom message = new(_userId, _roomId);

        return new(NetworkMessageType.JoinRoom, message.GetBytes());
    }

    public static NetworkPackage CreateCloseServerRequest(uint _userId)
    {
        CloseServer message = new(_userId);

        return new(NetworkMessageType.CloseServer, message.GetBytes());
    }

    public static NetworkPackage CreateLeaveRoomRequest(uint _userId)
    {
        LeaveRoom message = new(_userId);

        return new(NetworkMessageType.LeaveRoom, message.GetBytes());
    }

    public static NetworkPackage CreateReadyInTheRoomRequest(uint _userId)
    {
        ReadyInTheRoom message = new(_userId);

        return new(NetworkMessageType.ReadyInTheRoom, message.GetBytes());
    }

    public static NetworkPackage CreateStartGameRequest(uint _userId)
    {
        StartGame message = new(_userId);

        return new(NetworkMessageType.StartGame, message.GetBytes());
    }

    public static NetworkPackage CreateKickOutRoomRequest(uint _userId, uint _targetId)
    {
        KickOutRoom message = new(_userId, _targetId);

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
        { NetworkMessageType.CloseServer, CloseServer.GetData },
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
    public LeaveServer(uint userId) : base(NetworkMessageType.LeaveServer, userId) { }

    static public LeaveServer GetData(byte[] data)
    {
        return JsonUtility.FromJson<LeaveServer>(Encoding.ASCII.GetString(data, 0, data.Length));
    }
}

[Serializable]
public class CreateRoom : NetworkMessage
{
    public CreateRoom(uint userId) : base(NetworkMessageType.CreateRoom, userId) { }

    static public CreateRoom GetData(byte[] data)
    {
        return JsonUtility.FromJson<CreateRoom>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    // 4 client
    public uint roomId;
}

[Serializable]
public class JoinRoom : NetworkMessage
{
    public JoinRoom(uint userId, uint roomId) : base(NetworkMessageType.JoinRoom, userId)
    {
        this.roomId = roomId;
    }

    static public JoinRoom GetData(byte[] data)
    {
        return JsonUtility.FromJson<JoinRoom>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    // 4 server
    public uint roomId;

    // 4 cient
    // room data, players in the room
}

[Serializable]
public class CloseServer : NetworkMessage
{
    public CloseServer(uint userId) : base(NetworkMessageType.CloseServer, userId) { }
    static public CloseServer GetData(byte[] data)
    {
        return JsonUtility.FromJson<CloseServer>(Encoding.ASCII.GetString(data, 0, data.Length));
    }
}


// -----------------------------------------------
// -----------------------------------------------
// ------------REQUEST IN THE ROOM----------------
// -----------------------------------------------
// -----------------------------------------------

[Serializable]
public class LeaveRoom : NetworkMessage
{
    public LeaveRoom(uint userId) : base(NetworkMessageType.LeaveRoom, userId) { }

    static public LeaveRoom GetData(byte[] data)
    {
        return JsonUtility.FromJson<LeaveRoom>(Encoding.ASCII.GetString(data, 0, data.Length));
    }
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