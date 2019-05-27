using System;
using System.Threading.Tasks;
using Mochi.Async;

namespace Mochi.Sample.Async
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var count = 0;
            var finish = false;
            var heavyTask = Task.Delay(3000).ContinueWith(_ => "hogehoge");

            while (!finish)
            {
                (finish, count) = await MochiTask.Switch((finish, heavyTask, count),
                    async c =>
                    {
                        var text = await c.Case(c.Capture.heavyTask);
                        Console.WriteLine($"result: {text}");
                        return (true, default);
                    },
                    async c =>
                    {
                        await c.Case(Task.Delay(100));
                        Console.WriteLine($"now loading - {c.Capture.count}");
                        return (false, c.Capture.count + 1);
                    }
                );
            }
        }
    }
}
