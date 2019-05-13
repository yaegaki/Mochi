using System.IO;
using System.Text;

namespace Mochi
{
    public readonly struct Part
    {
        private readonly byte[] data;
        private readonly int offset;
        private readonly int count;
        public readonly string FileName;
        public readonly string ContentType;
        public int Size => count;

        public Part(byte[] data, int offset, int count, string fileName, string contentType)
        {
            this.data = data;
            this.offset = offset;
            this.count = count;
            this.FileName = fileName;
            this.ContentType = contentType;
        }

        public string GetValue()
        {
            return Encoding.UTF8.GetString(this.data, this.offset, this.count);
        }

        public Stream GetStream()
        {
            return new MemoryStream(this.data, this.offset, this.count, false);
        }
    }
}
