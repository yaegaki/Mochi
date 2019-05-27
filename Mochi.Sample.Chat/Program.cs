using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mochi.Sample.Chat
{
    class Client
    {
        public Queue<string> Queue { get; }
        public Mochi.Async.Signaler Signaler { get; }

        public Client(Queue<string> queue, Mochi.Async.Signaler signaler)
            => (this.Queue, this.Signaler) = (queue, signaler);
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var mochi = new Mochi.HTTPServer();

            var signaler = new Mochi.Async.Signaler();
            var fiber = new Mochi.Async.Fiber();
            var messages = new List<string>();
            var clients = new List<Client>();

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

                await fiber;
                messages.Add($"{name},{text}");
                signaler.Signal();

                await ctx.Response.WriteAsync("OK", ctx.CancellationToken);
            });

            mochi.Get("/sse", async ctx =>
            {
                ctx.Response.SetHeader("Access-Control-Allow-Origin", "*");
                ctx.Response.SetContentType("text/event-stream; charset=utf-8");

                var client = new Client(new Queue<string>(), new Mochi.Async.Signaler());

                await fiber;
                clients.Add(client);

                try
                {
                    while (true)
                    {
                        await client.Signaler.WaitSignalAsync(ctx.CancellationToken);

                        while (true)
                        {
                            await fiber;
                            if (client.Queue.Count == 0) break;
                            var text = client.Queue.Dequeue();
                            await ctx.Response.WriteAsync($"data: {text}\r\n\r\n", ctx.CancellationToken);
                            await ctx.Response.FlushAsync(ctx.CancellationToken);
                        }
                    }
                }
                finally
                {
                    await fiber;
                    clients.Remove(client);
                }
            });


            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await signaler.WaitSignalAsync(default(CancellationToken));

                    while (true)
                    {
                        await fiber;
                        if (messages.Count == 0) break;

                        foreach (var message in messages)
                        {
                            foreach (var client in clients)
                            {
                                client.Queue.Enqueue(message);
                            }
                        }

                        foreach (var client in clients)
                        {
                            client.Signaler.Signal();
                        }
                        messages.Clear();
                    }
                }
            });

            await mochi.StartServeAsync(new IPEndPoint(IPAddress.Loopback, 8080), CancellationToken.None);
        }
    }
}
