using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mochi.Internal;

namespace Mochi
{
    public class WebSocket
    {
        private readonly IAsyncStreamReader reader;
        private readonly IAsyncStreamWriter writer;

        public WebSocket(IAsyncStreamReader reader, IAsyncStreamWriter writer)
            => (this.reader, this.writer) = (reader, writer);

        public async Task<WebSocketResult> ReadAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            await this.reader.ReadBlockAsync(buffer, 0, 2, cancellationToken);

            if ((buffer[0] & 0x80) == 0)
            {
                throw new NotImplementedException("TODO: continuous frame");
            }

            var opCode = (WebSocketOpCode)(buffer[0] & 0x0f);
            switch (opCode)
            {
                case WebSocketOpCode.Text:
                case WebSocketOpCode.Binary:
                case WebSocketOpCode.Close:
                case WebSocketOpCode.Ping:
                case WebSocketOpCode.Pong:
                    break;
                default:
                    throw new InvalidReceiveDataException($"WebSocket: Unknown OpCode '{opCode}'...");
            }

            if ((buffer[1] & 0x80) == 0)
            {
                throw new InvalidReceiveDataException("WebSocket: Mask bit must be set");
            }

            var payloadLength = (int)(buffer[1] & 0x7f);
            if (payloadLength == 126)
            {
                await this.reader.ReadBlockAsync(buffer, 0, 2, cancellationToken);
                payloadLength = (int)BigEndianBitConverter.ReadUInt16BE(buffer, 0);
            }
            else if (payloadLength  == 127)
            {
                await this.reader.ReadBlockAsync(buffer, 0, 8, cancellationToken);
                var _payloadLength = BigEndianBitConverter.ReadUInt64BE(buffer, 0);
                if (_payloadLength >= (ulong)int.MaxValue)
                {
                    throw new InvalidReceiveDataException($"WebSocket: Too big payload ({_payloadLength} bytes)");
                }
                payloadLength = (int)_payloadLength;
            }

            // read maskkey
            await this.reader.ReadBlockAsync(buffer, 0, 4, cancellationToken);

            if (opCode == WebSocketOpCode.Binary)
            {
                Array.Resize(ref buffer, payloadLength + 4);
            }

            Decoder utf8Decoder = null;
            StringBuilder sb = null;
            char[] charBuffer = null;
            var remain = payloadLength;
            var buffAvailable = buffer.Length - 4;
            var offset = 0;
            while (remain > 0)
            {
                var read = Math.Min(remain, buffAvailable);

                // read payload
                await this.reader.ReadBlockAsync(buffer, 4, read, cancellationToken);
                remain -= read;

                for (var i = 0; i < read; i++)
                {
                    buffer[i+4] ^= buffer[(i+offset) % 4];
                }

                if (offset == 0)
                {
                    if (remain == 0)
                    {
                        switch (opCode)
                        {
                            case WebSocketOpCode.Text:
                                return new WebSocketResult(Encoding.UTF8.GetString(buffer, 4, read));
                            case WebSocketOpCode.Binary:
                                return new WebSocketResult(new ArraySegment<byte>(buffer, 4, read));
                            default:
                                return new WebSocketResult(opCode);
                        }
                    }

                    if (opCode == WebSocketOpCode.Text)
                    {
                        utf8Decoder = Encoding.UTF8.GetDecoder();
                        charBuffer = new char[10];
                        sb = new StringBuilder();
                    }
                }
                offset += read;

                if (utf8Decoder != null)
                {
                    int bytesUsed, charsUsed;
                    bool completed = false;
                    var convertBufferOffset = 4;
                    var convertByteCount = read;
                    while (!completed)
                    {
                        utf8Decoder.Convert(buffer, convertBufferOffset, convertByteCount, charBuffer, 0, charBuffer.Length, false, out bytesUsed, out charsUsed, out completed);
                        convertBufferOffset += bytesUsed;
                        convertByteCount -= bytesUsed;
                        sb.Append(charBuffer, 0, charsUsed);
                    }
                }
            }

            if (opCode == WebSocketOpCode.Text)
            {
                return new WebSocketResult(sb?.ToString() ?? string.Empty);
            }

            return new WebSocketResult(opCode);
        }

        public Task WriteAsync(string data, CancellationToken cancellationToken)
        {
            var bin = Encoding.UTF8.GetBytes(data);
            return WriteAsync(WebSocketOpCode.Text, bin, 0, bin.Length, cancellationToken);
        }

        public Task WriteAsync(byte[] data, int offset, int count, CancellationToken cancellationToken)
            => WriteAsync(WebSocketOpCode.Binary, data, offset, count, cancellationToken);

        public async Task WriteAsync(WebSocketOpCode opCode, byte[] data, int offset, int count, CancellationToken cancellationToken)
        {
            var buffer = new byte[10];

            // fin & opCode
            buffer[0] = (byte)(0x80 | ((byte)opCode & 0x0f));

            if (count <= 125)
            {
                buffer[1] = (byte)count;
                await this.writer.WriteAsync(buffer, 0, 2, cancellationToken);
            }
            else if (count <= ushort.MaxValue)
            {
                buffer[1] = 126;
                BigEndianBitConverter.WriteBE((ushort)count, buffer, 2);
                await this.writer.WriteAsync(buffer, 0, 4, cancellationToken);
            }
            else
            {
                buffer[1] = 127;
                BigEndianBitConverter.WriteBE((ulong)count, buffer, 2);
                await this.writer.WriteAsync(buffer, 0, 10, cancellationToken);
            }

            if (count > 0)
            {
                await this.writer.WriteAsync(data, offset, count, cancellationToken);
            }

            await this.writer.FlushAsync(cancellationToken);
        }
    }
}
