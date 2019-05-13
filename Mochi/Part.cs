using System.IO;
using System.Text;

namespace Mochi
{
    public readonly struct Part
    {
        private readonly byte[] data;
        private readonly int offset;
        private readonly int count;

        public string GetValue()
        {
            return Encoding.UTF8.GetString(this.data, this.offset, this.count);
        }

        public Stream GetFile()
        {
            return new MemoryStream(this.data, this.offset, this.count, false);
        }
    }
}
