using System;

namespace Mochi
{
    class InvalidReceiveDataException : Exception
    {
        public InvalidReceiveDataException() : base() {}
        public InvalidReceiveDataException(string message) : base(message) {}
    }
}
