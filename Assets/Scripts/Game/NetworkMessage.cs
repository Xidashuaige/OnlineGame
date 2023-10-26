using System.Net;

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
    Null
}

public class NetworkMessage
{
    protected NetworkMessage(NetworkMessageType type)
    {
        this.type = type;
    }

    // 4 server & client
    public NetworkMessageType type = NetworkMessageType.Null;

    // 4 client
    public bool succesful = false;
}


// -----------------------------------------------
// -----------------------------------------------
// ------------REQUEST IN THE SERVER--------------
// -----------------------------------------------
// -----------------------------------------------
public class JoinServer : NetworkMessage
{
    public JoinServer(IPEndPoint userIp, string userName) : base(NetworkMessageType.JoinServer)
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
    public LeaveServer(uint userId) : base(NetworkMessageType.LeaveServer)
    {
        id = userId;
    }

    // 4 server
    public uint id;
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