using System.Collections.Generic;
public class Room
{
    public Room(User roomMaster, int limitUser)
    {
        _users = new();
        _roomMaster = roomMaster;
        _users.Add(roomMaster);
        _limitUsers = limitUser;
    }

    public User RoomMaster { get { return _roomMaster; } }
    public List<User> Users { get { return _users; } }
    public int LimitUsers { get { return _limitUsers; } }

    private List<User> _users;
    private int _limitUsers = 2;
    private User _roomMaster;
}

public class RoomManager
{
    private List<Room> _rooms = new();

    public void CreateRoom(User roomMaster, int limitUser = 2)
    {
        _rooms.Add(new Room(roomMaster, limitUser));
    }
}
