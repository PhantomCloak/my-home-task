using System.Collections.Concurrent;

public class Room
{
    public Character MyCharacter;
    public ConcurrentDictionary<Character, PlayerSnapshot> PlayersInRoom;

    public Room()
    {
        PlayersInRoom = new();
    }
};

public static class SharedVariables
{
    public static Room CurrentMultiplayerRoom;
}
