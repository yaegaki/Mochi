namespace Mochi
{
    public readonly struct HijackResult
    {
        public readonly bool Ok;
        public readonly IAsyncStreamReader Reader;
        public readonly IAsyncStreamWriter Writer;

        public HijackResult(IAsyncStreamReader reader, IAsyncStreamWriter writer)
            => (this.Ok, this.Reader, this.Writer) = (true, reader, writer);
        
        public void Deconstruct(out bool ok, out IAsyncStreamReader reader, out IAsyncStreamWriter writer)
            => (ok, reader, writer) = (this.Ok, this.Reader, this.Writer);
    }
}
