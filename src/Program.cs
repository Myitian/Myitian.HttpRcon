using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Myitian.HttpRcon;

public class Program
{
    internal static readonly Dictionary<string, ConnectionInfo> ConnectionInfoMap = [];
    internal static ILogger? Logger;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);
        var app = builder.Build();
        app.MapPost("/connect", Connect).DisableAntiforgery();
        app.MapPost("/command", Command).DisableAntiforgery();
        app.MapPost("/command2", Command2).DisableAntiforgery();
        app.MapPost("/invalid", Invalid).DisableAntiforgery();
        app.MapPost("/close", Close).DisableAntiforgery();
        app.MapMethods("/", [HttpMethods.Get, HttpMethods.Head, HttpMethods.Post], Info).DisableAntiforgery();
        Logger = app.Logger;
        app.Run();
    }

    static IResult Connect(
        [FromForm] string password,
        [FromForm] string host = "localhost",
        [FromForm] ushort port = 25575)
    {
        string token = "";
        try
        {
            Logger?.LogInformation("[{time}] Try to connect to {host}:{port} ...", DateTime.Now, host, port);
            ConnectionInfo conn = new(host, port);
            token = conn.Token;
            Logger?.LogInformation("[{time}] Token: {token}", DateTime.Now, conn.Token);
            StreamWrapper stream = conn.Stream;
            Logger?.LogInformation("[{DateTime.Now}] Sending Login Packet", DateTime.Now);
            int id = Packet.SendLogin(stream, password);
            Logger?.LogInformation("[{DateTime.Now}] Receiving Login Response Packet", DateTime.Now);
            Packet packet = Packet.Receive(stream);
            if (packet.Type != TypeID.SERVERDATA_AUTH_RESPONSE)
            {
                Logger?.LogWarning("[{time}] Wrong Response Type!", DateTime.Now);
                return Results.Text("Wrong Response Type!", statusCode: StatusCodes.Status500InternalServerError);
            }
            else if (packet.PacketID == id)
            {
                return Results.Text(conn.Token, statusCode: StatusCodes.Status200OK);
            }
            else if (packet.PacketID < 0)
            {
                Logger?.LogWarning("[{time}] Wrong Password!", DateTime.Now);
                return Results.Text("Wrong Password!", statusCode: StatusCodes.Status403Forbidden);
            }
            else
            {
                Logger?.LogWarning("[{time}] Wrong Response ID!", DateTime.Now);
                return Results.Text("Wrong Response ID!", statusCode: StatusCodes.Status500InternalServerError);
            }
        }
        catch (EndOfStreamException)
        {
            if (ConnectionInfoMap.TryGetValue(token, out ConnectionInfo? conn))
            {
                conn.Dispose();
            }
            Logger?.LogError("Connection lost.");
            return Results.Text("Connection lost.", statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex.ToString());
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    static IResult Command(
        [FromForm] string command,
        [FromForm] string token)
    {
        try
        {
            if (ConnectionInfoMap.TryGetValue(token, out ConnectionInfo? conn) && conn.Check())
            {
                StreamWrapper stream = conn.Stream;
                Logger?.LogInformation("[{DateTime.Now}] Sending Command Packet", DateTime.Now);
                int id = Packet.SendCommand(stream, command);
                Logger?.LogInformation("[{DateTime.Now}] Sending Invalid Packet", DateTime.Now);
                Packet.SendInvalid(stream, 0);
                StringBuilder sb = new();
                do
                {
                    Logger?.LogInformation("[{DateTime.Now}] Receiving Command Response Packet", DateTime.Now);
                    Packet packet = Packet.Receive(stream);
                    if (packet.Type != TypeID.SERVERDATA_RESPONSE_VALUE)
                    {
                        Logger?.LogWarning("[{time}] Wrong Response Type!", DateTime.Now);
                        return Results.Text("Wrong Response Type!", statusCode: StatusCodes.Status500InternalServerError);
                    }
                    else if (packet.PacketID == id)
                    {
                        Logger?.LogInformation("[{DateTime.Now}] Received Command Response Packet", DateTime.Now);
                        sb.Append(packet.Payload);
                        continue;
                    }
                    else if (packet.PacketID != 0)
                    {
                        Logger?.LogWarning("[{time}] Wrong Response ID!", DateTime.Now);
                        return Results.Text("Wrong Response ID!", statusCode: StatusCodes.Status500InternalServerError);
                    }
                    else
                    {
                        Logger?.LogInformation("[{DateTime.Now}] Received Command Response Packet Ending", DateTime.Now);
                    }
                } while (false);
                return Results.Text(sb.ToString(), statusCode: StatusCodes.Status200OK);
            }
            else
            {
                Logger?.LogWarning("[{time}] Wrong Token: {token}", DateTime.Now, token);
                Logger?.LogDebug("[{time}] Token List: {tokens}", DateTime.Now, string.Join(',', ConnectionInfoMap.Keys));
                return Results.BadRequest();
            }
        }
        catch (EndOfStreamException)
        {
            if (ConnectionInfoMap.TryGetValue(token, out ConnectionInfo? conn))
            {
                conn.Dispose();
            }
            Logger?.LogError("Connection lost.");
            return Results.Text("Connection lost.", statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex.ToString());
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    static IResult Command2(
        [FromForm] string command,
        [FromForm] string token)
    {
        try
        {
            if (ConnectionInfoMap.TryGetValue(token, out ConnectionInfo? conn) && conn.Check())
            {
                StreamWrapper stream = conn.Stream;
                Logger?.LogInformation("[{DateTime.Now}] Sending Command Packet", DateTime.Now);
                int id = Packet.SendCommand(stream, command);
                StringBuilder sb = new();
                Logger?.LogInformation("[{DateTime.Now}] Receiving Command Response Packet", DateTime.Now);
                Packet packet = Packet.Receive(stream);
                if (packet.Type != TypeID.SERVERDATA_RESPONSE_VALUE)
                {
                    Logger?.LogWarning("[{time}] Wrong Response Type!", DateTime.Now);
                    return Results.Text("Wrong Response Type!", statusCode: StatusCodes.Status500InternalServerError);
                }
                else if (packet.PacketID == id)
                {
                    Logger?.LogInformation("[{DateTime.Now}] Received Command Response Packet", DateTime.Now);
                    return Results.Text(packet.Payload, statusCode: StatusCodes.Status200OK);
                }
                else
                {
                    Logger?.LogWarning("[{time}] Wrong Response ID!", DateTime.Now);
                    return Results.Text("Wrong Response ID!", statusCode: StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                Logger?.LogWarning("[{time}] Wrong Token: {token}", DateTime.Now, token);
                Logger?.LogDebug("[{time}] Token List: {tokens}", DateTime.Now, string.Join(',', ConnectionInfoMap.Keys));
                return Results.BadRequest();
            }
        }
        catch (EndOfStreamException)
        {
            if (ConnectionInfoMap.TryGetValue(token, out ConnectionInfo? conn))
            {
                conn.Dispose();
            }
            Logger?.LogError("Connection lost.");
            return Results.Text("Connection lost.", statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex.ToString());
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    static IResult Invalid([FromForm] string token)
    {
        try
        {
            if (ConnectionInfoMap.TryGetValue(token, out ConnectionInfo? conn) && conn.Check())
            {
                StreamWrapper stream = conn.Stream;
                Logger?.LogInformation("[{DateTime.Now}] Sending Invalid Packet", DateTime.Now);
                Packet.SendInvalid(stream, 0);
                Logger?.LogInformation("[{DateTime.Now}] Receiving Command Response Packet", DateTime.Now);
                Packet packet = Packet.Receive(stream);
                if (packet.Type != TypeID.SERVERDATA_RESPONSE_VALUE)
                {
                    Logger?.LogWarning("[{time}] Wrong Response Type!", DateTime.Now);
                    return Results.Text("Wrong Response Type!", statusCode: StatusCodes.Status500InternalServerError);
                }
                else if (packet.PacketID == 0)
                {
                    Logger?.LogInformation("[{DateTime.Now}] Receivinged Command Response Packet Ending", DateTime.Now);
                    return Results.Text(packet.Payload, statusCode: StatusCodes.Status200OK);
                }
                else
                {
                    Logger?.LogWarning("[{time}] Wrong Response ID!", DateTime.Now);
                    return Results.Text("Wrong Response ID!", statusCode: StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                Logger?.LogWarning("[{time}] Wrong Token: {token}", DateTime.Now, token);
                return Results.BadRequest();
            }
        }
        catch (EndOfStreamException)
        {
            if (ConnectionInfoMap.TryGetValue(token, out ConnectionInfo? conn))
            {
                conn.Dispose();
            }
            Logger?.LogError("Connection lost.");
            return Results.Text("Connection lost.", statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex.ToString());
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    static IResult Close([FromForm] string token)
    {
        try
        {
            if (ConnectionInfoMap.TryGetValue(token, out ConnectionInfo? conn))
            {
                conn.Dispose();
                return Results.Ok();
            }
            else
            {
                Logger?.LogWarning("[{time}] Wrong Token: {token}", DateTime.Now, token);
                return Results.BadRequest();
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex.ToString());
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    static IResult Info()
    {
        return Results.Text(@"使用方法
-
/connect 连接服务器
请求正文：password=密码&host=主机（可选，默认localhost）&port=端口（可选，默认25575）
响应：403（密码错误），500（其他错误，见响应正文），200（响应正文为Token）
-
/command 发送命令
* 会发送一个命令包和一个无效包，在服务器高负载时可能会导致连接中断
请求正文：token=Token&command=命令
响应：400（Token无效），500（其他错误，见响应正文），200（响应正文为RCON响应）
-
/command2 发送短回复命令
* 只会发送一个命令包，并只接收一个回包，如果返回内容过长，会导致后续无法正常收发包
请求正文：token=Token&command=命令
响应：400（Token无效），500（其他错误，见响应正文），200（响应正文为RCON响应）
-
/invalid 发送无效包
请求正文：token=Token
响应：400（Token无效），500（其他错误，见响应正文），200（响应正文为RCON响应）
-
/close 关闭连接
请求正文：token=Token
响应：400（Token无效），500（其他错误，见响应正文），200（响应正文为空）
-
Token有效期1H，每次发送命令/无效请求会刷新");
    }
}