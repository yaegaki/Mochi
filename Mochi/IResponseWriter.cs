using System.Threading;
using System.Threading.Tasks;

namespace Mochi
{
    public interface IResponseWriter
    {
        void SetHeader(string name, string value);
        /// <summary>
        /// Hijack connection.
        /// </summary>
        /// <returns></returns>
        HijackResult Hijack();
        Task WriteStatusCodeAsync(int statusCode, CancellationToken cancellationToken);
        Task WriteAsync(byte[] data, CancellationToken cancellationToken);
        Task WriteAsync(byte[] data, int offset, int count, CancellationToken cancellationToken);
        Task WriteAsync(string s, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
        Task FinishAsync(CancellationToken cancellationToken);
    }

    public static class IResponseWriterExtensions
    {
        public static void SetContentType(this IResponseWriter res, string contentType)
        {
            res.SetHeader(KnwonHeaders.ContentType, contentType);
        }
    }
}