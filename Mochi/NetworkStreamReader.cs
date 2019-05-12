using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mochi
{
    class NetworkStreamReader
    {
        private Stream stream;
        private byte[] buffer;
        private int offset;
        private int remain;

        public NetworkStreamReader(Stream stream, byte[] buffer)
            => (this.stream, this.buffer) = (stream, buffer);
        
        public async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var (found, line) = CheckBuffer();
                if (found) return line;

                var len = this.buffer.Length - this.remain;
                var read = await this.stream.ReadAsync(buffer, this.remain, len);
                if (read == 0)
                {
                    throw new InvalidReceiveDataException("Unexpected EOF");
                }

                this.remain += read;
            }
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset)
        {
            if (this.remain > 0)
            {
                var copyByte = Math.Min(this.remain, buffer.Length - offset);
                Array.Copy(this.buffer, this.offset, buffer, offset, copyByte);
                if (this.remain == copyByte)
                {
                    this.offset = 0;
                    this.remain = 0;
                }
                else
                {
                    this.offset += copyByte;
                    this.remain -= copyByte;
                }

                return copyByte;
            }

            return await this.stream.ReadAsync(buffer, offset, buffer.Length - offset);
        }

        public async Task ReadBlockAsync(byte[] buffer, int offset, int count)
        {
            if (this.remain > 0)
            {
                var copyByte = Math.Min(this.remain, count);
                Array.Copy(this.buffer, this.offset, buffer, offset, copyByte);
                if (this.remain == copyByte)
                {
                    this.offset = 0;
                    this.remain = 0;
                }
                else
                {
                    this.offset += copyByte;
                    this.remain -= copyByte;
                }

                offset += copyByte;
                count -= copyByte;
            }

            while (count > 0)
            {
                var read = await this.stream.ReadAsync(buffer, offset, count);
                if (read == 0) throw new InvalidReceiveDataException("Unexpected EOF");

                offset += read;
                count -= read;
            }
        }

        private (bool found, string line) CheckBuffer()
        {
            var buffer = this.buffer;
            var offset = this.offset;
            var remain = this.remain;
            var end = offset + remain;
            var lineByteCount = -1;
            var lineBreakByte = -1;

            for (var i = offset; i < end; i++)
            {
                if (buffer[i] == '\n')
                {
                    if (i > offset)
                    {
                        if (buffer[i - 1] == '\r')
                        {
                            lineByteCount = i - offset - 1;
                            lineBreakByte = 2;
                        }
                        else
                        {
                            lineByteCount = i - offset;
                            lineBreakByte = 1;
                        }
                    }
                    else
                    {
                        lineByteCount = 0;
                        lineBreakByte = 1;
                    }
                    break;
                }
            }

            if (lineByteCount < 0)
            {
                if (end == buffer.Length)
                {
                    if (offset != 0)
                    {
                        Array.Copy(buffer, offset, buffer, 0, remain);
                        this.offset = 0;
                    }
                    else
                    {
                        throw new BufferFullException();
                    }
                }

                return (false, string.Empty);
            }

            var totalLineByte = lineByteCount + lineBreakByte;
            int nextOffset;
            if (totalLineByte >= remain)
            {
                nextOffset = 0;
                remain = 0;
            }
            else
            {
                nextOffset = offset + totalLineByte;
                remain = remain - totalLineByte;
            }

            var line = lineByteCount == 0 ? string.Empty : Encoding.UTF8.GetString(buffer, offset, lineByteCount);

            this.offset = nextOffset;
            this.remain = remain;

            return (true, line);
        }
    }
}
