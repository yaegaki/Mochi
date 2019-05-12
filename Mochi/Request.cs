using System;
using System.Collections.Generic;
using System.Text;

namespace Mochi
{
    public struct Request
    {
        public string Path { get; }
        public Dictionary<string, string> Headers { get; }
        public byte[] Body { get; }

        public Dictionary<string, string> ParseBody()
        {
            var body = new Dictionary<string, string>();
            string contentType;
            if (!Headers.TryGetValue(KnwonHeaders.ContentType, out contentType)) return body;

            var sc = contentType.IndexOf(';');
            if (sc >= 0)
            {
                // only accept utf-8...
                var _contentType = contentType.Substring(0, sc);
                contentType = _contentType;
            }

            if (contentType != ContentTypes.ApplicationXWWWFormURLEncoded) return body;

            if (Body.Length == 0) return body;

            var str = Encoding.UTF8.GetString(Body);
            foreach (var param in str.Split('&'))
            {
                var xs = param.Split('=');
                if (xs.Length != 2) continue;
                var key = Uri.UnescapeDataString(xs[0].Replace('+', ' '));
                var value = Uri.UnescapeDataString(xs[1].Replace('+', ' '));
                body[key] = value;
            }
            return body;
        }

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