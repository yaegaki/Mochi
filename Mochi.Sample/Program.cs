using System;
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


        static async Task Main(string[] args)
        {
            var mochi = new Mochi.HTTPServer();
            mochi.Get("/", async ctx =>
            {
                await ctx.Response.WriteAsync(Template, ctx.CancellationToken);
            });

            await mochi.StartServeAsync(new IPEndPoint(IPAddress.Loopback, 8080), CancellationToken.None);
        }
    }
}
