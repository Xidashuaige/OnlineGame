using System;
using System.Net;
using System.Xml.Linq;

public enum NetWorkMessageType
{
    JoinServer,
    LeaveServer,
    JoinRoom,
    LeaveRoom,
    CreateRoom,
    PrepareInTheRoom,
    StartGame,
    KickOutRoom,
    Null
}

public class NetworkMessage
{
    protected NetworkMessage(NetWorkMessageType type)
    {
        this.type = type;
    }

    // 4 server & client
    public NetWorkMessageType type = NetWorkMessageType.Null;

    // 4 client
    public bool succesful = false;
}

public class JoinServer : NetworkMessage
{
    public JoinServer(IPEndPoint userIp, string userName) : base(NetWorkMessageType.JoinServer)
    {
        ip = userIp;
        name = userName;
    }

    // 4 server
    public IPEndPoint ip;
    public string name;

    // 4 client
    public uint id;
}

public class LeaveServer : NetworkMessage
{
    public LeaveServer(uint userId) : base(NetWorkMessageType.LeaveServer)
    {
        id = userId;
    }

    // 4 server
    public uint id;
}

public class JoinRoom : NetworkMessage
{
    public JoinRoom(uint userId, uint roomId) : base(NetWorkMessageType.JoinRoom)
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

public class LeaveRoom : NetworkMessage
{
    public LeaveRoom(uint userId) : base(NetWorkMessageType.LeaveRoom)
    {
        id = userId;
    }

    // 4 server
    public uint id;
}

