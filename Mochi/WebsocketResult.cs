using System;

namespace Mochi
{
    public readonly struct WebSocketResult
    {
        public readonly WebSocketOpCode OpCode;

        public readonly string Text;
        public readonly ArraySegment<byte> Binary;

        public WebSocketResult(WebSocketOpCode opCode)
        {
            this.OpCode = opCode;
            this.Text = default;
            this.Binary = default;
        }

        public WebSocketResult(string text)
        {
            this.OpCode = WebSocketOpCode.Text;
            this.Text = text;
            this.Binary = default;
        }

        public WebSocketResult(byte[] binary)
            : this(new ArraySegment<byte>(binary, 0, binary.Length))
        {
        }

        public WebSocketResult(ArraySegment<byte> binary)
        {
            this.OpCode = WebSocketOpCode.Binary;
            this.Text = default;
            this.Binary = binary;
        }
    }
}
