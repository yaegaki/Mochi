using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mochi
{
    public class NetworkStreamWriter
    {
        private Stream stream;
        private byte[] buffer;
        private int count;

        public NetworkStreamWriter(Stream stream, byte[] buffer)
        {
            this.stream = stream;
            this.buffer = buffer;
        }

        public Task WriteAsync(byte[] data, CancellationToken cancellationToken)
            => WriteAsync(data, 0, data.Length, cancellationToken);

        public async Task WriteAsync(byte[] data, int offset, int count, CancellationToken cancellationToken)
        {
            while (count > 0)
            {
                if (this.buffer.Length == this.count)
                {
                    await FlushAsync(cancellationToken);
                }

                var len = Math.Min(this.buffer.Length - this.count, count);

                Array.Copy(data, offset, this.buffer, this.count, len);
                offset += len;
                this.count += len;
                count -= len;
            }
        }

        public Task WriteAsync(string s, CancellationToken cancellationToken)
        {
            // TODO: improve(to less memory allocation.)
            var bytes = Encoding.UTF8.GetBytes(s);
            return WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }

        public async Task WriteAsync(byte b, CancellationToken cancellationToken)
        {
            if (this.buffer.Length == this.count && this.count > 0)
            {
                await FlushAsync(cancellationToken);
            }

            this.buffer[this.count] = b;
            this.count++;
        }

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            await this.stream.WriteAsync(this.buffer, 0, this.count, cancellationToken);
            this.count = 0;
        }
    }
}