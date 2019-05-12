using System.Threading;
using System.Threading.Tasks;

namespace Mochi
{
    public interface IResponseWriter
    {
        void SetHeader(string name, string value);
        Task WriteStatusCodeAsync(int statusCode, CancellationToken cancellationToken);
        Task WriteAsync(byte[] data, CancellationToken cancellationToken);
        Task WriteAsync(byte[] data, int offset, int count, CancellationToken cancellationToken);
        Task WriteAsync(string s, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
        Task FinishAsync(CancellationToken cancellationToken);
    }
}