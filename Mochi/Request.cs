using System;
using System.Collections.Generic;
using System.Text;

namespace Mochi
{
    public class Request
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
        public string Host { get; }
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
            this.Host = headers["Host"];
        }

        private Form ParseForm()
        {
            string contentType;
            if (!this.Headers.TryGetValue(KnwonHeaders.ContentType, out contentType))
            {
                return default;
            }

            var (mediaType, paramDict) = ParseMediaType(contentType);

            switch (mediaType)
            {
                case ContentTypes.MultiplartFormData:
                    return ParseMultiPartForm(paramDict);
                case ContentTypes.ApplicationXWWWFormURLEncoded:
                    return ParseWWWFormURLEncoded();
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

                paramDict[keyValue[0]] = keyValue[1].Trim('"');
            }

            return (mediaType, paramDict);
        }

        private Form ParseMultiPartForm(Dictionary<string, string> paramDict)
        {
            string boundary;
            if (paramDict == null || !paramDict.TryGetValue("boundary", out boundary) || boundary.Length == 0)
            {
                return default;
            }

            var byteCount = Encoding.UTF8.GetByteCount(boundary);
            // var boundaryBytes = Encoding.UTF8.GetBytes($"\r\n--{boundary}\r\n");
            var boundaryBytes = new byte[byteCount + 6];
            boundaryBytes[0] = (byte)'\r';
            boundaryBytes[1] = (byte)'\n';
            boundaryBytes[2] = boundaryBytes[3] = (byte)'-';
            boundaryBytes[boundaryBytes.Length - 2] = (byte)'\r';
            boundaryBytes[boundaryBytes.Length - 1] = (byte)'\n';
            Encoding.UTF8.GetBytes(boundary, 0, boundary.Length, boundaryBytes, 4);

            if (Body.Length < boundaryBytes.Length) return default;

            // check first boundary line.
            if (IndexOfBytes(Body, 0, boundaryBytes, 2, boundaryBytes.Length - 2) != 0)
            {
                return default;
            }

            Dictionary<string, List<string>> valuesDict = null;
            Dictionary<string, List<Part>> partsDict = null;
            var offset = boundaryBytes.Length - 2;
            while (true)
            {
                var (nextOffset, name, part) = ParsePart(Body, offset, boundaryBytes);
                // found invalid part.
                if (nextOffset < 0) return default;

                if (string.IsNullOrEmpty(part.FileName))
                {
                    if (valuesDict == null)
                    {
                        valuesDict = new Dictionary<string, List<string>>();
                    }

                    List<string> values;
                    if (!valuesDict.TryGetValue(name, out values))
                    {
                        values = new List<string>();
                        valuesDict[name] = values;
                    }

                    values.Add(part.GetValue());
                }
                else
                {
                    if (partsDict == null)
                    {
                        partsDict = new Dictionary<string, List<Part>>();
                    }

                    List<Part> parts;
                    if (!partsDict.TryGetValue(name, out parts))
                    {
                        parts = new List<Part>();
                        partsDict[name] = parts;
                    }

                    parts.Add(part);
                }

                if (Body.Length - nextOffset <= 4)
                {
                    break;
                }

                // next offset is here |
                //                     v
                //         --{boundary}\r\n

                // check crlf.
                if (Body[nextOffset] != boundaryBytes[0] || Body[nextOffset + 1] != boundaryBytes[1])
                {
                    return default;
                }

                offset = nextOffset + 2;
            }


            return new Form(valuesDict, partsDict);
        }

        private (int nextOffset, string name, Part part) ParsePart(byte[] body, int headerOffset, byte[] boundaryBytes)
        {
            var (dataOffset, headers) = ParsePartHeaders(body, headerOffset, boundaryBytes);
            if (dataOffset < 0) return (-1, string.Empty, default);

            // Not implemented...
            if (headers.ContainsKey("Content-Transfer-Encoding"))
            {
                return (-1, string.Empty, default);
            }

            string contentDisposition;
            if (!headers.TryGetValue("Content-Disposition", out contentDisposition))
            {
                return (-1, string.Empty, default);
            }

            var (mediaType, paramDict) = ParseMediaType(contentDisposition);
            string name;
            if (mediaType != "form-data" || paramDict == null || !paramDict.TryGetValue("name", out name))
            {
                return (-1, string.Empty, default);
            }

            string fileName;
            if (!paramDict.TryGetValue("filename", out fileName))
            {
                fileName = string.Empty;
            }

            string contentType;
            if (headers.TryGetValue("Content-Type", out contentType))
            {
                var d = contentType.IndexOf(';');
                if (d >= 0)
                {
                    contentType = contentType.Substring(0, d);
                }
            }
            else
            {
                contentType = ContentTypes.TextPlane;
            }

            var dataEnd = IndexOfBytes(body, dataOffset, boundaryBytes, 0, boundaryBytes.Length - 2);
            if (dataEnd < 0) return (-1, string.Empty, default);

            var len = dataEnd - dataOffset;

            return (dataEnd + boundaryBytes.Length - 2, name, new Part(body, dataOffset, len, fileName, contentType));
        }

        private (int dataOffset, Dictionary<string, string> headers) ParsePartHeaders(byte[] body, int offset, byte[] boundaryBytes)
        {
            Dictionary<string, string> headers = null;
            while (true)
            {
                var index = IndexOfBytes(body, offset, boundaryBytes, 0, 2);
                if (index < 0)
                {
                    return (-1, null);
                }

                // empty line.
                if (index == offset)
                {
                    // missing header.
                    if (headers == null)
                    {
                        return (-1, null);
                    }

                    return (index + 2, headers);
                }

                var line = Encoding.UTF8.GetString(body, offset, index - offset);
                var d = line.IndexOf(':');
                // invalid header.
                if (d <= 0) return (-1, null);
                var name = line.Substring(0, d);
                var value = line.Substring(d + 1).Trim();

                if (headers == null)
                {
                    headers = new Dictionary<string, string>();
                }

                // dupulicate parameter name.
                if (headers.ContainsKey(name)) return (-1, null);
                headers[name] = value;
                offset = index + 2;
            }
        }

        private Form ParseWWWFormURLEncoded()
        {
            if (Body.Length == 0) return default;
            var valuesDict = new Dictionary<string, List<string>>();

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

            return new Form(valuesDict, null);
        }

        private int IndexOfBytes(byte[] target, int offset, byte[] data, int dataOffset, int dataCount)
        {
            var end = target.Length - data.Length;
            for (var i = offset; i < end; i++)
            {
                var found = true;
                for (var j = 0; j < dataCount; j++)
                {
                    if (target[i + j] != data[j + dataOffset])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}