using System.Threading;
using System.Threading.Tasks;

namespace Mochi
{
    public interface IAsyncStreamWriter
    {
        Task WriteAsync(byte[] data, int offset, int count, CancellationToken cancellationToken);
        Task WriteAsync(string data, CancellationToken cancellationToken);
        Task WriteAsync(byte data, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
    }
}
