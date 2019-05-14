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
            var signaler = new Mochi.Async.Signaler<string>();

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
            es.addEventListener('message', e => {
                var m = e.data.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/""/g, '&quot;').replace(/'/g, '&#39;');
                chat.innerHTML = `<div>${m}</div>${chat.innerHTML}`;
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
                signaler.Signal(ctx.Reqeust.Form.GetValue("text"));
                await ctx.Response.WriteAsync("OK", ctx.CancellationToken);
            });

            mochi.Get("/sse", async ctx =>
            {
                ctx.Response.SetHeader("Access-Control-Allow-Origin", "*");
                ctx.Response.SetContentType("text/event-stream; charset=utf-8");

                var runner = new Mochi.Async.SequentialTaskRunner();

                var signalTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        var text = await signaler.WaitSignaleAsync(ctx.CancellationToken);
                        runner.Enqueue(Task.Run(async () =>
                        {
                            await ctx.Response.WriteAsync($"data: {text}\r\n\r\n", ctx.CancellationToken);
                            await ctx.Response.FlushAsync(ctx.CancellationToken);
                        }));
                    }
                });

                await Task.WhenAny(signalTask, runner.Run());
            });

            await mochi.StartServeAsync(new IPEndPoint(IPAddress.Loopback, 8080), CancellationToken.None);
        }
    }
}
