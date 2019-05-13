using System;
using System.Collections.Generic;
using System.Text;

namespace Mochi
{
    public struct Request
    {
        private bool isFormParsed;
        private Form form;
        public Form Form
        {
            get
            {
                if (!isFormParsed)
                {
                    form = ParseForm();
                    isFormParsed = true;
                }

                return form;
            }
        }
        public string Path { get; }
        public Dictionary<string, string> Headers { get; }
        public byte[] Body { get; }
        public Request(
            string path,
            Dictionary<string, string> headers,
            byte[] body
        )
        {
            this.isFormParsed = false;
            this.form = default;
            this.Path = path;
            this.Headers = headers;
            this.Body = body;
        }

        private Form ParseForm()
        {
            string contentType;
            if (!this.Headers.TryGetValue(KnwonHeaders.ContentType, out contentType))
            {
                return default;
            }

            var (mediaType, parameters) = ParseMediaType(contentType);

            switch (mediaType)
            {
                case ContentTypes.ApplicationXWWWFormURLEncoded:
                    return new Form(ParseWWWFormURLEncoded(), null);
                default:
                    return default;
            }
        }

        private (string mediaType, Dictionary<string , string> paramDict) ParseMediaType(string contentType)
        {
            var d = contentType.IndexOf(';');
            string mediaType;
            if (d < 0)
            {
                return (contentType, null);
            }

            var xs = contentType.Split(';');
            mediaType = xs[0];
            Dictionary<string, string> paramDict = null;
            for (var i = 1; i < xs.Length; i++)
            {
                var x = xs[i];
                var keyValue = x.Trim().Split('=');
                if (keyValue.Length != 2) continue;

                if (paramDict == null)
                {
                    paramDict = new Dictionary<string, string>();
                }

                paramDict[keyValue[0]] = keyValue[1];
            }

            return (mediaType, paramDict);
        }

        private Dictionary<string, List<string>> ParseWWWFormURLEncoded()
        {
            var valuesDict = new Dictionary<string, List<string>>();
            if (Body.Length == 0) return valuesDict;

            var str = Encoding.UTF8.GetString(Body);
            foreach (var param in str.Split('&'))
            {
                var xs = param.Split('=');
                if (xs.Length != 2) continue;
                var key = Uri.UnescapeDataString(xs[0].Replace('+', ' '));
                var value = Uri.UnescapeDataString(xs[1].Replace('+', ' '));
                List<string> values;
                if (!valuesDict.TryGetValue(key, out values))
                {
                    values = new List<string>();
                    valuesDict[key] = values;
                }

                values.Add(value);
            }

            return valuesDict;
        }
    }
}