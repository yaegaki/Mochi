using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mochi
{
    public class HTTPServer
    {
        public event Action<Exception> OnClientException;
        public Func<Context, Task> getHandler;
        public Func<Context, Task> postHandler;

        public Task StartServeAsync(IPEndPoint endpoint, CancellationToken cancellationToken)
            => Task.Run(async () => await StartServeAsyncCore(endpoint, cancellationToken));

        public void Get(string path, Func<Context, Task> handler)
        {
            getHandler = handler;
        }
        
        public void Post(string path, Func<Context, Task> handler)
        {
            postHandler = handler;
        }

        private async Task StartServeAsyncCore(IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            using (var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                serverSocket.Bind(endpoint);
                serverSocket.Listen(10);

                while (true)
                {
                    var clientSocket = await Task.Run(async () => await serverSocket.AcceptAsync(), cancellationToken);
                    _ = Task.Run(async () =>
                    {
                        using (var ns = new NetworkStream(clientSocket, true))
                        {
                            try
                            {
                                await ServeAsync(ns, cancellationToken);
                            }
                            catch (InvalidReceiveDataException)
                            {
                                // ignore
                            }
                            catch (BufferFullException)
                            {
                                // ignore
                            }
                        }
                    })
                    .ContinueWith(t => this.OnClientException?.Invoke(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
                }
            }
        }

        private async Task ServeAsync(NetworkStream ns, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            var sr = new NetworkStreamReader(ns, buffer);

            // RequestLine: ex) GET / HTTP/1.1
            var requestLine = await sr.ReadLineAsync(cancellationToken);
            var (isValid, method, path, version) = ParseRequestLine(requestLine);
            if (!isValid) throw new InvalidReceiveDataException($"Invalid RequestLine: '{requestLine}'");

            // Console.WriteLine($"Method:'{method}' Path:'{path}' Version:'{version}'");
            var headers = new Dictionary<string, string>();
            while (true)
            {
                var line = await sr.ReadLineAsync(cancellationToken);
                if (line.Length == 0) break;
                
                string name, value;
                (isValid, name, value) = ParseHeader(line);
                if (!isValid) throw new InvalidReceiveDataException($"Invalid Header: '{line}'");
                if (headers.ContainsKey(name)) throw new InvalidReceiveDataException($"Duplicate Header: '{name}'");

                headers.Add(name, value);
               //  Console.WriteLine($"Header:{name}, {value}");
            }

            byte[] body = Array.Empty<byte>();
            if (headers.TryGetValue("Content-Length", out string contentLengthStr))
            {
                int contentLength;
                if (!int.TryParse(contentLengthStr, out contentLength) || contentLength < 0)
                {
                    throw new InvalidReceiveDataException($"Invalid Content-Length: {contentLengthStr}");
                }

                if (contentLength > 1024 * 1024)
                {
                    throw new InvalidReceiveDataException($"Too large Content-Length: {contentLengthStr}");
                }

                // Console.WriteLine($"Content-Length:{contentLength}");
                if (contentLength > 0)
                {
                    var bodyBuffer = new byte[contentLength];
                    await sr.ReadBlockAsync(bodyBuffer, 0, contentLength);
                    body = bodyBuffer;
                }
            }

            var writeBuffer = new byte[1024];
            var context = new Context(
                new Request(path, headers, body),
                new ResponseWriter(new NetworkStreamWriter(ns, writeBuffer)),
                cancellationToken
            );

            Task handleTask;
            switch (method)
            {
                case "GET":
                    handleTask = this.getHandler?.Invoke(context);
                    break;
                case "POST":
                    handleTask = this.postHandler?.Invoke(context);
                    break;
                default:
                    throw new NotImplementedException($"NotImplementedMethod : '{method}'");
            }

            if (handleTask != null)
            {
                await handleTask;
            }

            await context.Response.FinishAsync(cancellationToken);
        }

        private (bool isValid, string method, string path, string version) ParseRequestLine(string line)
        {
            var first = line.IndexOf(' ');
            if (first < 0) return (false, string.Empty, string.Empty, string.Empty);
            var second = line.IndexOf(' ', first + 1);
            if (second < 0) return (false, string.Empty, string.Empty, string.Empty);

            return (
                true,
                line.Substring(0, first),
                line.Substring(first + 1, second - (first + 1)),
                line.Substring(second + 1)
            );
        }

        private (bool isValid, string name, string value) ParseHeader(string line)
        {
            var colon = line.IndexOf(':');
            if (colon <= 0) return (false, string.Empty, string.Empty);

            var valueStartOffset = colon + 1;
            for (; valueStartOffset < line.Length && line[valueStartOffset] == ' '; valueStartOffset++);

            return (
                true,
                line.Substring(0, colon),
                valueStartOffset >= line.Length ? string.Empty : line.Substring(valueStartOffset)
            );
        }
    }
}