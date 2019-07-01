using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Mochi.Internal
{
    public static class WebSocketUpgrader
    {
        public static void Handle(HTTPServer server, string path, Func<WebSocketContext, Task> handleFunc)
        {
            server.Get(path, async ctx =>
            {
                var req = ctx.Reqeust;
                var headers = req.Headers;

                string host, upgrade, connection, websocketKey, websocketVersion;
                if (!headers.TryGetValue("Host", out host) ||
                    !headers.TryGetValue("Upgrade", out upgrade) ||
                    !headers.TryGetValue("Connection", out connection) ||
                    !headers.TryGetValue("Sec-WebSocket-Key", out websocketKey) ||
                    !headers.TryGetValue("Sec-WebSocket-Version", out websocketVersion)
                )
                {
                    await ctx.Response.WriteStatusCodeAsync(400, ctx.CancellationToken);
                    await ctx.Response.WriteAsync("Invalid Request.", ctx.CancellationToken);
                    return;
                }

                if (upgrade != "websocket" || connection != "Upgrade")
                {
                    await ctx.Response.WriteStatusCodeAsync(400, ctx.CancellationToken);
                    await ctx.Response.WriteAsync("invalid protocol", ctx.CancellationToken);
                    return;
                }

                var (ok, sr, sw) = ctx.Response.Hijack();

                if (!ok)
                {
                    await ctx.Response.WriteStatusCodeAsync(500, ctx.CancellationToken);
                    await ctx.Response.WriteAsync("internal server error.", ctx.CancellationToken);
                    return;
                }

                var sha1 = SHA1.Create();
                var keyBin = Encoding.UTF8.GetBytes(websocketKey+"258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
                var hash = sha1.ComputeHash(keyBin);
                var digest = Convert.ToBase64String(hash);
                await sw.WriteAsync("HTTP/1.1 101 Switching Protocols\r\n", ctx.CancellationToken);
                await sw.WriteAsync("Upgrade: websocket\r\n", ctx.CancellationToken);
                await sw.WriteAsync("Connection: Upgrade\r\n", ctx.CancellationToken);
                await sw.WriteAsync($"Sec-WebSocket-Accept: {digest}\r\n\r\n", ctx.CancellationToken);
                await sw.FlushAsync(ctx.CancellationToken);

                await handleFunc(new WebSocketContext(ctx, new WebSocket(sr, sw)));
            });
        }
    }
}
