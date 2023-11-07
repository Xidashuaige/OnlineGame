using System.Net;
using System.Text;
using System.Xml.Linq;
using UnityEditor.VersionControl;
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
    Null
}

public class NetworkMessage
{
    protected NetworkMessage(NetworkMessageType type)
    {
        this.type = type;
    }

    public byte[] GetBytes()
    {
        return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
    }

    // 4 server & client
    public NetworkMessageType type = NetworkMessageType.Null;

    // 4 client
    public bool succesful = false;

    public EndPoint endPoint = null;
}

public class HearthBeat : NetworkMessage
{
    public HearthBeat(uint userid) : base(NetworkMessageType.Heartbeat)
    {
        id = userid;
    }

    public uint id;
}

// -----------------------------------------------
// -----------------------------------------------
// ------------REQUEST IN THE SERVER--------------
// -----------------------------------------------
// -----------------------------------------------
public class JoinServer : NetworkMessage
{
    public JoinServer(string userName) : base(NetworkMessageType.JoinServer)
    {
        name = userName;
    }

    // 4 server
    public string name;

    // 4 client
    public uint id;
}

public class LeaveServer : NetworkMessage
{
    public LeaveServer(uint userId) : base(NetworkMessageType.LeaveServer)
    {
        id = userId;
    }

    // 4 server
    public uint id;
}

public class CreateRoom: NetworkMessage
{
    public CreateRoom(uint userId) : base(NetworkMessageType.CreateRoom)
    {
        this.userId = userId;
    }

    // 4 server
    public uint userId;

    // 4 client
    public uint roomId;
}

public class JoinRoom : NetworkMessage
{
    public JoinRoom(uint userId, uint roomId) : base(NetworkMessageType.JoinRoom)
    {
        this.userId = userId;
        this.roomId = roomId;
    }

    // 4 server
    public uint userId;
    public uint roomId;

    // 4 cient
    // room data, players in the room
}


// -----------------------------------------------
// -----------------------------------------------
// ------------REQUEST IN THE ROOM----------------
// -----------------------------------------------
// -----------------------------------------------
public class LeaveRoom : NetworkMessage
{
    public LeaveRoom(uint userId) : base(NetworkMessageType.LeaveRoom)
    {
        id = userId;
    }

    // 4 server
    public uint id;
}

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
public class KickOutRoom : NetworkMessage
{
    public KickOutRoom(uint userId, uint targetUserId):base(NetworkMessageType.KickOutRoom)
    {
        id = userId;
        this.targetUserId  = targetUserId;
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