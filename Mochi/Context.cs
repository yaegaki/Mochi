using System.Net.Sockets;
using System.Threading;

namespace Mochi
{
    public readonly struct Context
    {
        public readonly Socket Socket;
        public readonly Request Reqeust;
        public readonly IResponseWriter Response;
        public readonly CancellationToken CancellationToken;

        public Context(Socket socket, Request request, ResponseWriter response, CancellationToken cancellationToken)
        {
            this.Socket = socket;
            this.Reqeust = request;
            this.Response = response;
            this.CancellationToken = cancellationToken;
        }
    }
}
