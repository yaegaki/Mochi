using System;

namespace Mochi
{
    class BufferFullException : Exception
    {
        public BufferFullException() : base() {}
        public BufferFullException(string message) : base(message) {}
    }
}
