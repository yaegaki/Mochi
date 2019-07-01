using System.Threading;

namespace Mochi
{
    public readonly struct WebSocketContext
    {
        public readonly Context ParentContext;
        public readonly WebSocket Socket;

        public CancellationToken CancellationToken => ParentContext.CancellationToken;

        public WebSocketContext(Context context, WebSocket socket)
            => (this.ParentContext, this.Socket) = (context, socket);
    }
}
