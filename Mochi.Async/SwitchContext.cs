using System.Threading;

namespace Mochi.Async
{
    public static class SwitchContext
    {
        public static SwitchContext<TCapture> Create<TCapture>(TCapture capture, CancellationToken cancellationToken, int index = default)
            => new SwitchContext<TCapture>(capture, cancellationToken, index);
    }

    public struct SwitchContext<TCapture>
    {
        public TCapture Capture { get; }
        public CancellationToken CancellationToken { get; }
        public int Index { get; internal set; }

        public SwitchContext(TCapture capture, CancellationToken cancellationToken, int index = default)
            => (this.Capture, this.CancellationToken, this.Index) = (capture, cancellationToken, index);
    }
}
