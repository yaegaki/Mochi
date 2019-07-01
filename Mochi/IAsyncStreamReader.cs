using System.Threading;
using System.Threading.Tasks;

namespace Mochi
{
    public interface IAsyncStreamReader
    {
        Task<string> ReadLineAsync(CancellationToken cancellationToken);
        Task<int> ReadAsync(byte[] buffer, int offset, CancellationToken cancellationToken);
        Task ReadBlockAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    }
}
