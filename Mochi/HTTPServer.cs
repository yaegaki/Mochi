using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Mochi.Internal;

namespace Mochi
{
    public class HTTPServer
    {
        public event Action<Exception> OnClientException;
        private Router router = new Router();

        public void Get(string path, Func<Context, Task> handleFunc)
            => this.router.Register(HTTPMethod.Get, path, handleFunc);
        
        public void Post(string path, Func<Context, Task> handleFunc)
            => this.router.Register(HTTPMethod.Post, path, handleFunc);
        
        public void WebSocket(string path, Func<WebSocketContext, Task> handleFunc)
            => WebSocketUpgrader.Handle(this, path, handleFunc);

        public async Task StartServeAsync(IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            using (var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (cancellationToken.Register(() => serverSocket.Close()))
            {
                serverSocket.Bind(endpoint);
                serverSocket.Listen(10);

                await Task.Run(async () =>
                {
                    while (true)
                    {
                        var clientSocket = await Task.Run(async () => await serverSocket.AcceptAsync(), cancellationToken);
                        _ = Task.Run(async () =>
                        {
                            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                            var linkedCancellationToken = linkedTokenSource.Token;

                            // check connection.
                            _ = Task.Run(async () =>
                            {
                                while (true)
                                {
                                    await Task.Delay(5000, linkedCancellationToken);
                                    if (clientSocket.Connected)
                                    {
                                        if (!clientSocket.Poll(1, SelectMode.SelectRead) || clientSocket.Available != 0)
                                        {
                                            continue;
                                        }
                                    }

                                    var _linkedTokenSource = linkedTokenSource;
                                    if (_linkedTokenSource != null)
                                    {
                                        if (Interlocked.CompareExchange(ref linkedTokenSource, null, _linkedTokenSource) == _linkedTokenSource)
                                        {
                                            _linkedTokenSource.Cancel();
                                            _linkedTokenSource.Dispose();
                                        }
                                    }
                                    break;
                                }
                            });

                            try
                            {
                                using (var ns = new NetworkStream(clientSocket, true))
                                {
                                    try
                                    {
                                        await ServeAsync(clientSocket, ns, linkedCancellationToken);
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
                            }
                            catch
                            {
                                var _linkedTokenSource = linkedTokenSource;
                                if (_linkedTokenSource != null)
                                {
                                    if (Interlocked.CompareExchange(ref linkedTokenSource, null, _linkedTokenSource) == _linkedTokenSource)
                                    {
                                        _linkedTokenSource.Cancel();
                                        _linkedTokenSource.Dispose();
                                    }
                                }
                            }
                        })
                        .ContinueWith(t => this.OnClientException?.Invoke(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
                    }
                });
            }
        }

        private async Task ServeAsync(Socket socket, NetworkStream ns, CancellationToken cancellationToken)
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

            HTTPMethod _method;
            switch (method)
            {
                case "GET":
                    _method = HTTPMethod.Get;
                    break;
                case "POST":
                    _method = HTTPMethod.Post;
                    break;
                default:
                    throw new NotImplementedException($"NotImplementedMethod : '{method}'");
            }

            byte[] body = Array.Empty<byte>();
            if (_method == HTTPMethod.Post && headers.TryGetValue(KnwonHeaders.ContentLength, out string contentLengthStr))
            {
                int contentLength;
                if (!int.TryParse(contentLengthStr, out contentLength) || contentLength < 0)
                {
                    throw new InvalidReceiveDataException($"Invalid Content-Length: {contentLengthStr}");
                }

                if (contentLength > 2 * 1024 * 1024)
                {
                    throw new InvalidReceiveDataException($"Too large Content-Length: {contentLengthStr}");
                }

                // Console.WriteLine($"Content-Length:{contentLength}");
                if (contentLength > 0)
                {
                    var bodyBuffer = new byte[contentLength];
                    await sr.ReadBlockAsync(bodyBuffer, 0, contentLength, cancellationToken);
                    body = bodyBuffer;
                }
            }

            var writeBuffer = new byte[1024];
            var context = new Context(
                socket,
                new Request(path, headers, body),
                new ResponseWriter(sr, new NetworkStreamWriter(ns, writeBuffer)),
                cancellationToken
            );

            var handleFunc = this.router.Find(_method, path);
            if (handleFunc == null)
            {
                await context.Response.WriteStatusCodeAsync(404, cancellationToken);
                await context.Response.WriteAsync("Not found.", cancellationToken);
            }
            else
            {
                await handleFunc(context);
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