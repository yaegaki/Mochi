using System;

namespace Mochi
{
    public class InvalidReceiveDataException : Exception
    {
        public InvalidReceiveDataException() : base() {}
        public InvalidReceiveDataException(string message) : base(message) {}
    }
}
