using System.Text;

namespace Myitian.HttpRcon;

public struct Packet(int type, string payload, int? id = null)
{
    public int Length;
    public int PacketID = id ?? Random.Shared.Next(1, 128);
    public int Type = type;
    public string Payload = payload;

    public static int Send(StreamWrapper stream, Packet packet)
    {
        Program.Logger?.LogDebug("Start Sending");
        int len = Encoding.UTF8.GetByteCount(packet.Payload) + 10;
        Program.Logger?.LogDebug("Write Length: {len}", len);
        stream.WriteInt(len);
        Program.Logger?.LogDebug("Write ID: {id}", packet.PacketID);
        stream.WriteInt(packet.PacketID);
        Program.Logger?.LogDebug("Write Type: {len}", packet.Type);
        stream.WriteInt(packet.Type);
        Program.Logger?.LogDebug("Write Payload: {payload}", packet.Payload);
        stream.WriteString(packet.Payload, Encoding.UTF8);
        Program.Logger?.LogDebug("Write Null");
        stream.WriteNull();
        return packet.PacketID;
    }

    public static int SendInvalid(StreamWrapper stream, int id)
    {
        stream.WriteInt(10);
        stream.WriteInt(id);
        stream.WriteInt(127);
        stream.WriteNull();
        stream.WriteNull();
        return id;
    }

    public static int SendLogin(StreamWrapper stream, string password)
    {
        return Send(stream, new(TypeID.SERVERDATA_AUTH, password));
    }

    public static int SendCommand(StreamWrapper stream, string command)
    {
        return Send(stream, new(TypeID.SERVERDATA_EXECCOMMAND, command));
    }

    public static Packet Receive(StreamWrapper stream)
    {
        Program.Logger?.LogDebug("Start Receiving");
        int len = stream.ReadInt();
        Program.Logger?.LogDebug("Read Length: {len}", len);
        int id = stream.ReadInt();
        Program.Logger?.LogDebug("Read ID: {id}", id);
        int type = stream.ReadInt();
        Program.Logger?.LogDebug("Read Type: {type}", type);
        string payload = stream.ReadString(Encoding.UTF8, len - 10);
        Program.Logger?.LogDebug("Read Payload: {payload}", payload);
        stream.ReadNull();
        Program.Logger?.LogDebug("Read Null");
        return new(type, payload, id) { Length = len };
    }
}