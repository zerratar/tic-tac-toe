public class Packet
{
    public Packet(GameClient client, string command)
    {
        Client = client;
        Command = command;
    }

    public GameClient Client { get; }
    public string Command { get; }
}