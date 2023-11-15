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
        return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    public NetworkMessage GetData()
    {
        switch (type)
        {
            case NetworkMessageType.Heartbeat:
                return HearthBeat.GetData(data);
        }

        return null;
    }

    public NetworkMessageType type;

    public byte[] data;

    //static Dictionary<NetworkMessageType, >
}

[Serializable]
public class NetworkMessage
{
    protected NetworkMessage(NetworkMessageType type)
    {
        this.type = type;
    }

    virtual public byte[] GetBytes()
    {
        return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    // 4 server & client
    public NetworkMessageType type = NetworkMessageType.Null;

    // 4 client
    public bool succesful = false;

    public EndPoint endPoint = null;
}

[Serializable]
public class HearthBeat : NetworkMessage
{
    public HearthBeat(uint userid) : base(NetworkMessageType.Heartbeat)
    {
        id = userid;
    }

    public override byte[] GetBytes()
    {
        return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    static public HearthBeat GetData(byte[] data)
    {
        return JsonUtility.FromJson<HearthBeat>(Encoding.ASCII.GetString(data, 0, data.Length));
    }

    public uint id;
}

// -----------------------------------------------
// -----------------------------------------------
// ------------REQUEST IN THE SERVER--------------
// -----------------------------------------------
// -----------------------------------------------

[Serializable]
public class JoinServer : NetworkMessage
{
    public JoinServer(string userName) : base(NetworkMessageType.JoinServer)
    {
        name = userName;
    }

    public override byte[] GetBytes()
    {
        return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    // 4 server
    public string name;

    // 4 client
    public uint id;
}

[Serializable]
public class LeaveServer : NetworkMessage
{
    public LeaveServer(uint userId) : base(NetworkMessageType.LeaveServer)
    {
        id = userId;
    }

    public override byte[] GetBytes()
    {
        return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    // 4 server
    public uint id;
}

[Serializable]
public class CreateRoom : NetworkMessage
{
    public CreateRoom(uint userId) : base(NetworkMessageType.CreateRoom)
    {
        this.userId = userId;
    }
    public override byte[] GetBytes()
    {
        return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    // 4 server
    public uint userId;

    // 4 client
    public uint roomId;
}

[Serializable]
public class JoinRoom : NetworkMessage
{
    public JoinRoom(uint userId, uint roomId) : base(NetworkMessageType.JoinRoom)
    {
        this.userId = userId;
        this.roomId = roomId;
    }

    public override byte[] GetBytes()
    {
        return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    // 4 server
    public uint userId;
    public uint roomId;

    // 4 cient
    // room data, players in the room
}

[Serializable]
public class CloseServer : NetworkMessage
{
    public CloseServer(uint userId) : base(NetworkMessageType.CloseServer)
    {
        this.userId = userId;
    }

    // 4 server
    public uint userId;
}


// -----------------------------------------------
// -----------------------------------------------
// ------------REQUEST IN THE ROOM----------------
// -----------------------------------------------
// -----------------------------------------------

[Serializable]
public class LeaveRoom : NetworkMessage
{
    public LeaveRoom(uint userId) : base(NetworkMessageType.LeaveRoom)
    {
        id = userId;
    }

    // 4 server
    public uint id;
}

[Serializable]
public class ReadyInTheRoom : NetworkMessage
{
    public ReadyInTheRoom(uint userId) : base(NetworkMessageType.ReadyInTheRoom)
    {
        id = userId;
    }

    // 4 server
    public uint id;
}

// Just for Room Master
[Serializable]
public class StartGame : NetworkMessage
{
    public StartGame(uint userId) : base(NetworkMessageType.StartGame)
    {
        id = userId;
    }

    // 4 server
    public uint id;
}

// Just for Room Master
[Serializable]
public class KickOutRoom : NetworkMessage
{
    public KickOutRoom(uint userId, uint targetUserId) : base(NetworkMessageType.KickOutRoom)
    {
        id = userId;
        this.targetUserId = targetUserId;
    }

    // 4 server
    public uint id;

    // 4 server & client
    public uint targetUserId;
}

// -----------------------------------------------
// -----------------------------------------------
// ------------REQUEST IN THE GAME----------------
// -----------------------------------------------
// -----------------------------------------------