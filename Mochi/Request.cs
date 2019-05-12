using System;
using System.Collections.Generic;

namespace Mochi
{
    public struct Request
    {
        public string Path { get; }
        public Dictionary<string, string> Headers { get; }
        public byte[] Body { get; }

        public Request(
            string path,
            Dictionary<string, string> headers,
            byte[] body
        )
        {
            this.Path = path;
            this.Headers = headers;
            this.Body = body;
        }
    }
}