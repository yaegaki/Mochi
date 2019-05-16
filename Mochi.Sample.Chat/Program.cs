using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mochi.Sample.Chat
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var mochi = new Mochi.HTTPServer();
            var postChannel = new Mochi.Async.Channel<string>();
            var clientConnectChannel = new Mochi.Async.Channel<Mochi.Async.Channel<string>>();
            var clientDisconnectChannel = new Mochi.Async.Channel<Mochi.Async.Channel<string>>();

            Func<Mochi.Context, Task> RenderFileHandler(string path)
            {
                return async ctx =>
                {
                    var ext = System.IO.Path.GetExtension(path).ToLower();
                    switch (ext)
                    {
                        case ".htm":
                        case ".html":
                            ctx.Response.SetContentType(ContentTypes.TextHtml);
                            break;
                        case ".js":
                            ctx.Response.SetContentType(ContentTypes.ApplicationJavaScript);
                            break;
                        default:
                            break;
                    }

                    // every time read file from disk(for debug.)
                    var data = await System.IO.File.ReadAllBytesAsync(path, ctx.CancellationToken);
                    await ctx.Response.WriteAsync(data, ctx.CancellationToken);
                };
            }

            // render static files.
            mochi.Get("/", RenderFileHandler("./public/index.html"));
            mochi.Get("/main.js", RenderFileHandler("./public/main.js"));

            mochi.Post("/post", async ctx =>
            {
                var name = ctx.Reqeust.Form.GetValue("name");
                var text = ctx.Reqeust.Form.GetValue("text");
                if (string.IsNullOrEmpty(name) || name.Contains(',') || name.Contains(':')) {
                    await ctx.Response.WriteStatusCodeAsync(400, ctx.CancellationToken);
                    await ctx.Response.WriteAsync("Bad Request", ctx.CancellationToken);
                    return;
                }


                await postChannel.SendAsync($"{name},{text}", ctx.CancellationToken);
                await ctx.Response.WriteAsync("OK", ctx.CancellationToken);
            });

            mochi.Get("/sse", async ctx =>
            {
                ctx.Response.SetHeader("Access-Control-Allow-Origin", "*");
                ctx.Response.SetContentType("text/event-stream; charset=utf-8");

                var receiveChannel = new Mochi.Async.Channel<string>(10);
                try
                {
                    await clientConnectChannel.SendAsync(receiveChannel, ctx.CancellationToken);


                    while (true)
                    {
                        var (text, _) = await receiveChannel.ReceiveAsync(ctx.CancellationToken);
                        await ctx.Response.WriteAsync($"data: {text}\r\n\r\n", ctx.CancellationToken);
                        await ctx.Response.FlushAsync(ctx.CancellationToken);
                    }
                }
                finally
                {
                    await clientDisconnectChannel.SendAsync(receiveChannel, CancellationToken.None);
                    receiveChannel.Dispose();
                }
            });


            _ = Task.Run(async () =>
            {
                var clients = new List<Mochi.Async.Channel<string>>();
                while (true)
                {
                    var result = await Mochi.Async.Channel.SelectAsync(postChannel, clientConnectChannel, clientDisconnectChannel, false, CancellationToken.None);
                    switch (result.Index)
                    {
                        case 0:
                            Console.WriteLine("Posted.");  
                            foreach (var client in clients) await client.SendAsync(result.Item1.Result, CancellationToken.None);
                            break;
                        case 1:
                            Console.WriteLine("Connected.");  
                            clients.Add(result.Item2.Result);
                            break;
                        case 2:
                            Console.WriteLine("Disconnected.");  
                            clients.Remove(result.Item3.Result);
                            break;
                        default:
                            throw new Exception("unknown bug.");
                    }
                }
            });

            await mochi.StartServeAsync(new IPEndPoint(IPAddress.Loopback, 8080), CancellationToken.None);
        }
    }
}
