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

            mochi.Get("/", async ctx =>
            {
                await ctx.Response.WriteAsync(@"<html>
    <head>
        <meta charset=""utf-8"">
        <title>Mochi Chat</title>
        <style>
            #chat {
                overflow-y: auto;
                height: 300px;
                border: 1px solid gray;
            }

            .error {
                color: red;
            }

            .flush {
                color: yellow;
            }
        </style>
    </head>
    <body>
        <div>Mochi Chat Server</div>
        <div id=""chat""></div>
        <form id=""form"" action=""/post"" method=""post"" enctype=""multipart/form-data"">
            <input id=""text"" name=""text"">
            <button>submit</button>
        </form>
        <script>
            form.addEventListener('submit', e => {
                e.preventDefault();

                if (text.value.length === 0) return;

                const form = new FormData();
                form.append('text', text.value);

                fetch('/post', { method: 'POST', body: form });
                text.value = '';
            });

            const es = new EventSource('/sse');
            const texts = [];
            es.addEventListener('message', e => {
                var m = e.data.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/""/g, '&quot;').replace(/'/g, '&#39;');
                texts.unshift(`<div>${m}</div>`);
                requestAnimationFrame(() => {
                    if (chat.innerHTML.length > 5000) chat.innerHTML = '<div class=""flush"">Flush...</div>';

                    chat.innerHTML = `<div>${texts.join('')}</div>${chat.innerHTML}`;
                    texts.length = 0;
                })
            });

            es.addEventListener('error', e => {
                chat.innerHTML = `<div class=""error"">Error occurred!</div>${chat.innerHTML}`;
            });
        </script>
    </body>
</html>", ctx.CancellationToken);
            });

            mochi.Post("/post", async ctx =>
            {
                await postChannel.SendAsync(ctx.Reqeust.Form.GetValue("text"), ctx.CancellationToken);
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
