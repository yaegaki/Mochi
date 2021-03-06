﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mochi.Sample
{
    class Program
    {
        private static readonly string Template = @"<html>
    <body>
        <h1>Welcome to MochiServer!</h1>
    </body>
</html>";

        private static readonly string FallbackTemplate = @"<html>
    <body>
        <h1>Welcome to MochiServer(FallbackPage)!</h1>
    </body>
</html>";


        static async Task Main(string[] args)
        {
            var mochi = new Mochi.HTTPServer();

            mochi.Get("/", async ctx =>
            {
                await ctx.Response.WriteAsync(Template, ctx.CancellationToken);
            });

            mochi.Get("/echo", async ctx =>
            {
                ctx.Response.SetContentType(ContentTypes.TextPlane);
                await ctx.Response.WriteStatusCodeAsync(200, ctx.CancellationToken);
                foreach (var pair in ctx.Reqeust.Headers)
                {
                    await ctx.Response.WriteAsync($"{pair.Key}: {pair.Value}\n", ctx.CancellationToken);
                }
            });

            mochi.Get("/form", async ctx =>
            {
                await ctx.Response.WriteAsync(@"<body>
    <form action=""/post"" method=""post"">
        <div><input name=""text""></div>
        <div><input name=""text""></div>
        <button>submit</button>
    </form>
</body>", ctx.CancellationToken);
            });

            mochi.Post("/post", async ctx =>
            {
                ctx.Response.SetContentType(ContentTypes.TextPlane);
                await ctx.Response.WriteStatusCodeAsync(200, ctx.CancellationToken);
                foreach (var (name, values) in ctx.Reqeust.Form.EnumerateAllValues())
                {
                    await ctx.Response.WriteAsync($"{name}: {string.Join(',', values)}\n", ctx.CancellationToken);
                }
            });

            mochi.Get("/*", async ctx =>
            {
                await ctx.Response.WriteAsync(FallbackTemplate, ctx.CancellationToken);
            });

            await mochi.StartServeAsync(new IPEndPoint(IPAddress.Loopback, 8080), CancellationToken.None);
        }
    }
}
