using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Mochi.Sample.WebSocket
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var mochi = new HTTPServer();
            mochi.Get("/", async ctx =>
            {
                await ctx.Response.WriteAsync(@"<html>
    <body>
        <h1>hello, websocket</h1>
        <script>
            const ws = new WebSocket('ws://localhost:8080/ws');
            ws.binaryType = 'arraybuffer';
            ws.onmessage = m => {
                console.log(m.data);
            };

            ws.onopen = () => {
                ws.send('hello');
                ws.send(new Uint8Array([0, 1, 2, 3, 4, 5, 6]));
            };
        </script>
    </body>
</html>", ctx.CancellationToken);
            });

            mochi.WebSocket("/ws", async ctx =>
            {
                var sock = ctx.Socket;
                
                while (true)
                {
                    var res = await sock.ReadAsync(ctx.CancellationToken);
                    switch (res.OpCode)
                    {
                        case WebSocketOpCode.Text:
                            await sock.WriteAsync(res.Text, ctx.CancellationToken);
                            break;
                        case WebSocketOpCode.Binary:
                            await sock.WriteAsync(res.Binary.Array, res.Binary.Offset, res.Binary.Count, ctx.CancellationToken);
                            break;
                        default:
                            break;
                    }
                }
            });

            await mochi.StartServeAsync(new IPEndPoint(IPAddress.Loopback, 8080), default);
        }
    }
}
