using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mochi
{
    public class ResponseWriter : IResponseWriter
    {
        private static readonly byte[] HTTPVersionBytes = Encoding.UTF8.GetBytes("HTTP/1.1");
        private static readonly byte[] OKBytes = Encoding.UTF8.GetBytes("OK");
        private static readonly byte[] LineBrakeBytes = Encoding.UTF8.GetBytes("\r\n");

        private NetworkStreamWriter sw;
        private Dictionary<string, string> headers = new Dictionary<string, string>();
        private bool isHeaderWrote;

        public ResponseWriter(NetworkStreamWriter sw)
        {
            this.sw = sw;
        }

        public void SetHeader(string name, string value)
        {
            if (this.isHeaderWrote)
            {
                throw new System.Exception("TODO");
            }

            this.headers[name] = value;
        }

        public async Task WriteStatusCodeAsync(int statusCode, CancellationToken cancellationToken)
        {
            if (this.isHeaderWrote)
            {
                throw new System.Exception("TODO");
            }

            // ex) HTTP/1.1 200 OK
            await this.sw.WriteAsync(HTTPVersionBytes, cancellationToken);
            await this.sw.WriteAsync((byte)' ', cancellationToken);
            await this.sw.WriteAsync(statusCode.ToString(), cancellationToken);
            await this.sw.WriteAsync((byte)' ', cancellationToken);
            await this.sw.WriteAsync(LineBrakeBytes, cancellationToken);

            string contentType;
            if (!this.headers.TryGetValue(KnwonHeaders.ContentType, out contentType))
            {
                contentType = ContentTypes.TextHtml;
            }

            await this.sw.WriteAsync($"Content-Type: {contentType}\r\n", cancellationToken);

            foreach (var pair in this.headers)
            {
                if (pair.Key == KnwonHeaders.ContentType) continue;

                await this.sw.WriteAsync($"{pair.Key}: {pair.Value}\r\n", cancellationToken);
            }

            await this.sw.WriteAsync(LineBrakeBytes, cancellationToken);


            this.isHeaderWrote = true;
        }

        public Task WriteAsync(byte[] data, CancellationToken cancellationToken)
            => WriteAsync(data, 0, data.Length, cancellationToken);

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!this.isHeaderWrote)
            {
                await WriteStatusCodeAsync(200, cancellationToken);
            }

            await this.sw.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public async Task WriteAsync(string s, CancellationToken cancellationToken)
        {
            if (!this.isHeaderWrote)
            {
                await WriteStatusCodeAsync(200, cancellationToken);
            }

            await this.sw.WriteAsync(s, cancellationToken);
        }

        public Task FlushAsync(CancellationToken cancellationToken)
            => this.sw.FlushAsync(cancellationToken);

        public async Task FinishAsync(CancellationToken cancellationToken)
        {
            if (!this.isHeaderWrote)
            {
                await WriteStatusCodeAsync(200, cancellationToken);
            }

            await FlushAsync(cancellationToken);
        }
    }
}
