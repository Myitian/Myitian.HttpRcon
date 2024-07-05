using System.Net.Sockets;

namespace Myitian.HttpRcon;

public class ConnectionInfo : IDisposable
{
    public DateTime ExpirationTime;
    public readonly string Token;
    public readonly TcpClient Tcp;
    public readonly StreamWrapper Stream;
    public ConnectionInfo(string host, ushort port)
    {
        ExpirationTime = DateTime.UtcNow + TimeSpan.FromHours(1);
        Token = Guid.NewGuid().ToString();
        Program.ConnectionInfoMap[Token] = this;
        Tcp = new(host, port);
        Stream = new(Tcp.GetStream());
    }
    public bool Check()
    {
        if (!Tcp.Connected || ExpirationTime < DateTime.UtcNow)
        {
            Program.Logger?.LogWarning("[{time}] Token Expired At {exp}. Current: {now}", DateTime.Now, ExpirationTime, DateTime.UtcNow);
            Dispose();
            return false;
        }
        else
        {
            ExpirationTime = DateTime.UtcNow + TimeSpan.FromHours(1);
            Program.Logger?.LogInformation("[{time}] Updated Token Expiration Time to {exp}", DateTime.Now, ExpirationTime);
            return true;
        }
    }

    public void Dispose()
    {
        Tcp.Dispose();
        Program.ConnectionInfoMap.Remove(Token);
        GC.SuppressFinalize(this);
    }
}