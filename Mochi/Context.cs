using System.Net.Sockets;
using System.Threading;

namespace Mochi
{
    public struct Context
    {
        public Socket Socket { get; }
        public Request Reqeust { get; }
        public IResponseWriter Response { get; }
        public CancellationToken CancellationToken { get; }

        public Context(Socket socket, Request request, ResponseWriter response, CancellationToken cancellationToken)
        {
            this.Socket = socket;
            this.Reqeust = request;
            this.Response = response;
            this.CancellationToken = cancellationToken;
        }
    }
}
