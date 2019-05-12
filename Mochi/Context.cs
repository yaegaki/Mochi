using System.Threading;

namespace Mochi
{
    public struct Context
    {
        public Request Reqeust { get; }
        public IResponseWriter Response { get; }
        public CancellationToken CancellationToken { get; }

        public Context(Request request, ResponseWriter response, CancellationToken cancellationToken)
        {
            this.Reqeust = request;
            this.Response = response;
            this.CancellationToken = cancellationToken;
        }
    }
}
